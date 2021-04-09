using Libs.GOAP;
using Libs.Looting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class LootGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.4f; }

        private ILogger logger;
        private readonly WowInput wowInput;

        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly ClassConfiguration classConfiguration;
        private readonly NpcNameFinder npcNameFinder;

        private bool debug = true;
        private bool outOfCombat = false;
        private bool targetSkinnable = false;

        public LootGoal(ILogger logger, WowInput wowInput, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving, ClassConfiguration classConfiguration, NpcNameFinder npcNameFinder)
        {
            this.logger = logger;
            this.wowInput = wowInput;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;

            this.classConfiguration = classConfiguration;
            this.npcNameFinder = npcNameFinder;

            outOfCombat = playerReader.PlayerBitValues.PlayerInCombat;
        }

        public virtual void AddPreconditions()
        {
            AddPrecondition(GoapKey.shouldloot, true);
        }

        public override bool CheckIfActionCanRun()
        {
            return !bagReader.BagsFull && playerReader.ShouldConsumeCorpse;
        }

        public override void ResetBeforePlanning()
        {
            outOfCombat = playerReader.PlayerBitValues.PlayerInCombat;
            base.ResetBeforePlanning();
        }

        public override async Task PerformAction()
        {
            WowPoint lastPosition = playerReader.PlayerLocation;
            targetSkinnable = false;

            Log("Stopped Movement for Loot Goals");
            await stopMoving.Stop();

            await Wait(100);
            Log("Interacting with Last Target for Loot");
            if (!this.playerReader.HasTarget)
            {
                await wowInput.TapLastTargetKey("Targeting last Target - Loot Goal");
            }
            await wowInput.TapInteractKey("Interacting with last Target - Loot Goal");


            await Wait(100);
            do
            {
                //attempts to preserve Last Target after looting
                if (classConfiguration.Skin)
                {

                    if (this.playerReader.HasTarget)
                    {
                        Log("Skinning last target to save Last Target for Skinning");
                        await wowInput.TapSkinningKey("Skinning target to preserve Last Target");
                        await wowInput.TapSkinningKey("Skinning target to preserve Last Target");
                    }
                    else
                    {
                        Log("Could not access Last Target while interacting with it, did we lose it?");
                        await wowInput.TapLastTargetKey("Targeting last Target - Checking Skinning Goal");
                    }
                }

                Log("Moving - Interact required movement to corpse");
                lastPosition = playerReader.PlayerLocation;

                if (!await Wait(100, DidEnterCombat()))
                {
                    await AquireTarget();
                    return;
                }
            } while (IsPlayerMoving(lastPosition));
            Log("Done Moving");


            await Wait(100);
            if (classConfiguration.Skin)
            {
                if (!this.playerReader.HasTarget)
                {
                    Log("Targeting Last Target to check for Skinning");
                    await wowInput.TapLastTargetKey("Targeting last Target - Checking Skinning Goal");
                }
                if (this.playerReader.HasTarget)
                {
                    Log("Casting Skinning to check if skinnable");
                    await wowInput.TapSkinningKey("Skinning target for a Spell Failed Lua Error");
                    await Wait(100);
                    if (classConfiguration.SkinningSpellIds.Contains(this.playerReader.SpellBeingCast))
                    {
                        Log("Casting Skinning - Target is Skinnable");
                        targetSkinnable = true;
                        await wowInput.TapStopAttack();
                    }
                    else
                    {
                        this.playerReader.UpdateLuaError();
                        await Wait(100);
                        if (this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_CREATURE_NOT_SKINNABLE)
                        {
                            Log("Last Target Skinning produced 'Not Skinnable' Lua Error");
                            targetSkinnable = false;
                        }
                        else if (this.playerReader.LastUIErrorMessage == UI_ERROR.ERR_CREATURE_NEEDS_LOOTED)
                        {
                            Log("Last Target Skinning needs to be looted");
                            await this.PerformAction();
                        }
                        Log("Not sure what happened");
                    }


                }
                else
                {
                    Log("Unable to Target Last Target after looting");
                    targetSkinnable = false;
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

        public async Task<bool> DidEnterCombat()
        {
            await Task.Delay(0);
            if (!outOfCombat && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Leaving Combat");
                outOfCombat = true;
                return false;
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
                    SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
                    SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                    SendActionEvent(new ActionEventArgs(GoapKey.hastarget, true));
                    return;
                }

                await wowInput.TapNearestTarget();
                if (this.playerReader.HasTarget && playerReader.PlayerBitValues.TargetInCombat)
                {
                    if (this.playerReader.TargetTarget == TargetTargetEnum.TargetIsTargettingMe)
                    {
                        Log("Found from nearest target");
                        SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
                        SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                        SendActionEvent(new ActionEventArgs(GoapKey.hastarget, true));
                        return;
                    }
                }

                await wowInput.TapClearTarget();
                Log("No target found");
            }
        }

        private bool IsPlayerMoving(WowPoint lastPos)
        {
            var distance = WowPoint.DistanceTo(lastPos, playerReader.PlayerLocation);
            return distance > 0.5f;
        }


        private async Task GoalExit()
        {
            AddEffect(GoapKey.shouldloot, false);
            SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));

            if (targetSkinnable)
            {
                AddEffect(GoapKey.shouldskin, targetSkinnable);
                SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, targetSkinnable));

            }
            await wowInput.TapStopAttack();
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }
    }
}