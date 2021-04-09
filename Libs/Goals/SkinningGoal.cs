using Libs.GOAP;
using Libs.Looting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class SkinningGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.6f; }

        private ILogger logger;
        private readonly WowInput wowInput;

        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly ClassConfiguration classConfiguration;
        private readonly NpcNameFinder npcNameFinder;


        private bool outOfCombat = false;

        public SkinningGoal(ILogger logger, WowInput wowInput, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving, ClassConfiguration classConfiguration, NpcNameFinder npcNameFinder)
        {
            this.logger = logger;
            this.wowInput = wowInput;

            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;

            this.classConfiguration = classConfiguration;
            this.npcNameFinder = npcNameFinder;

            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.shouldskin, true);

            AddEffect(GoapKey.shouldskin, false);

            outOfCombat = playerReader.PlayerBitValues.PlayerInCombat;
        }

        public override bool CheckIfActionCanRun()
        {
            return !bagReader.BagsFull &&
                playerReader.ShouldConsumeCorpse &&
                (
                bagReader.HasItem(7005) ||
                bagReader.HasItem(12709) ||
                bagReader.HasItem(19901)
                );
        }

        public override async Task PerformAction()
        {
            WowPoint lastPosition = playerReader.PlayerLocation;
            this.playerReader.LastUIErrorMessage = UI_ERROR.NONE;

            Log("Stopped movement for Skinning Goals");
            await stopMoving.Stop();
            await Wait(500);


            if (!this.playerReader.HasTarget)
            {
                Log("Attempting to Target Last Target");
                await wowInput.TapLastTargetKey("Targeting last Target - Skinning Goal");
            }

            if (this.playerReader.HasTarget)
            {
                await wowInput.TapInteractKey("Interacting(Skinning) the last Target - Skinning Goal");
                Log("Interacting(Skinning) with Last Target");
                while (IsPlayerMoving(lastPosition))
                {
                    Log("Moving - Skinning required movement to corpse");
                    lastPosition = playerReader.PlayerLocation;
                    if (!await Wait(500, DidEnterCombat()))
                    {
                        await AquireTarget();
                        return;
                    }
                }
                do
                {
                    if (!await Wait(100, DidEnterCombat()))
                    {
                        await AquireTarget();
                        return;
                    }
                } while (playerReader.IsCasting);

                // Wait for to update the LastUIErrorMessage
                this.playerReader.UpdateLuaError();
                await Wait(100);
                var lastError = this.playerReader.LastUIErrorMessage;
                switch (lastError)
                {
                    case UI_ERROR.NONE:
                        logger.LogDebug("Skinning Interaction resulted in no errors");
                        break;
                    case UI_ERROR.ERR_CREATURE_NOT_SKINNABLE:
                        Log("Last Target was not skinnable");
                        break;
                    case UI_ERROR.ERR_SKILL_NOT_HIGH_ENOUGH:
                    case UI_ERROR.ERR_USE_LOCKED_WITH_SPELL_KNOWN_SI:
                        Log("Skinning skill not high enough to skin");
                        break;
                    case UI_ERROR.ERR_CREATURE_NEEDS_LOOTED:
                        Log("Last Target needs to be looted before Skinning");
                        break;
                    case UI_ERROR.ERR_SPELL_FAILED_S:
                        Log("Skinning cast failed, reattempting Skinning");
                        await this.PerformAction();
                        break;
                    //Target is tapped? dont think this should matter as it can be skinned anyways as long as looted
                    default:
                        Log($"Unknown edge case: {lastError}");
                        break;
                }
            }

            await GoalExit();
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (sender != this)
            {
                outOfCombat = true;
            }
        }

        async Task GoalExit()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, false));

            npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.Enemy);

            await wowInput.TapClearTarget();
        }

        public async Task<bool> DidEnterCombat()
        {
            await Task.Delay(0);
            if (!outOfCombat && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Leaving Combat");
                outOfCombat = true;
            }

            if (outOfCombat && playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Entering Combat");
                return true;
            }

            return false;
        }

        private async Task AquireTarget()
        {
            if (this.playerReader.PlayerBitValues.PlayerInCombat && this.playerReader.PetHasTarget)
            {
                await wowInput.TapTargetPet();
                Log($"Pets target {this.playerReader.TargetTarget}");
                if (this.playerReader.TargetTarget == TargetTargetEnum.PetHasATarget)
                {
                    Log("Found target by pet");
                    await wowInput.TapTargetOfTarget();
                    SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                    return;
                }

                await wowInput.TapNearestTarget();
                if (this.playerReader.HasTarget && playerReader.PlayerBitValues.TargetInCombat)
                {
                    if (this.playerReader.TargetTarget == TargetTargetEnum.TargetIsTargettingMe)
                    {
                        Log("Found from nearest target");
                        SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                        return;
                    }
                }

                await wowInput.TapClearTarget();
                Log("No target found");
            }
        }



        public override void ResetBeforePlanning()
        {
            base.ResetBeforePlanning();
        }

        private void Log(string text)
        {
            logger.LogInformation($"{this.GetType().Name}: {text}");
        }

        private bool IsPlayerMoving(WowPoint lastPos)
        {
            var distance = WowPoint.DistanceTo(lastPos, playerReader.PlayerLocation);
            return distance > 0.5f;
        }
    }
}
