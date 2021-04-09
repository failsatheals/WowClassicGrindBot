﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Libs
{
    public class BadZone
    {
        public int ZoneId { get; set; } = -1;
        public WowPoint ExitZoneLocation { get; set; } = new WowPoint(0, 0);
    }

    public enum Mode
    {
        Grind = 0,
        CorpseRun = 1,
        AttendedGather = 2,
        AttendedGrind = 3
    }


    public class ClassConfiguration
    {
        public string ClassName { get; set; } = string.Empty;
        public bool Loot { get; set; } = true;
        public bool Skin { get; set; } = true;
        public bool UseMount { get; set; } = true;

        public string PathFilename { get; set; } = string.Empty;
        public string SpiritPathFilename { get; set; } = string.Empty;

        public string? OverridePathFilename { get; set; } = string.Empty;

        public bool PathThereAndBack { get; set; } = true;
        public bool PathReduceSteps { get; set; } = false;

        public Mode Mode { get; set; } = Mode.Grind;

        public BadZone WrongZone { get; set; } = new BadZone();

        public int NPCMaxLevels_Above { get; set; } = 1;
        public int NPCMaxLevels_Below { get; set; } = 7;
        public List<string> Blacklist { get; } = new List<string>();

        public Dictionary<int, List<SchoolMask>> ImmunityBlacklist { get; } = new Dictionary<int, List<SchoolMask>>();

        public KeyActions Pull { get; set; } = new KeyActions();
        public KeyActions Combat { get; set; } = new KeyActions();
        public KeyActions Adhoc { get; set; } = new KeyActions();
        public KeyActions Parallel { get; set; } = new KeyActions();
        public KeyActions NPC { get; set; } = new KeyActions();

        public List<KeyAction> ShapeshiftForm { get; } = new List<KeyAction>();
        public List<KeyAction> GatherFindKeyConfig { get; } = new List<KeyAction>();
        public List<string> GatherFindKeys { get; } = new List<string>();

        public KeyAction Jump { get; set; } = new KeyAction();
        public string JumpKey { get; set; } = "Spacebar";

        public KeyAction Interact { get; set; } = new KeyAction();
        public string InteractKey { get; set; } = "H";

        public KeyAction TargetLastTarget { get; set; } = new KeyAction();
        public string TargetLastTargetKey { get; set; } = "G";

        public KeyAction StandUp { get; set; } = new KeyAction();
        public string StandUpKey { get; set; } = "X";

        public KeyAction ClearTarget { get; set; } = new KeyAction();
        public string ClearTargetKey { get; set; } = "Insert";

        public KeyAction StopAttack { get; set; } = new KeyAction();
        public string StopAttackKey { get; set; } = "Delete";

        public KeyAction TargetNearestTarget { get; set; } = new KeyAction();
        public string TargetNearestTargetKey { get; set; } = "Tab";

        public KeyAction TargetTargetOfTarget { get; set; } = new KeyAction();
        public string TargetTargetOfTargetKey { get; set; } = "F";
        public KeyAction TargetPet { get; set; } = new KeyAction();
        public string TargetPetKey { get; set; } = "Multiply";

        public KeyAction PetAttack { get; set; } = new KeyAction();
        public string PetAttackKey { get; set; } = "Subtract";
        public KeyAction Skinning { get; set; } = new KeyAction();
        public string SkinningKey { get; set; } = "I";

        public KeyAction Mount { get; set; } = new KeyAction();
        public string MountKey { get; set; } = "Y";

        public KeyAction Hearthstone { get; set; } = new KeyAction();
        public string HearthstoneKey { get; set; } = "J";
        public List<long> SkinningSpellIds { get; } = new List<long>() { 8613, 8617, 10768, 8618 };


        public static Dictionary<ShapeshiftForm, ConsoleKey> ShapeshiftFormKeys
        { get; private set; } = new Dictionary<ShapeshiftForm, ConsoleKey>();

        public void Initialise(DataConfig dataConfig, PlayerReader playerReader, RequirementFactory requirementFactory, ILogger logger, string? overridePathProfileFile)
        {
            SpiritPathFilename = string.Empty;

            Pull.Initialise(playerReader, requirementFactory, logger);
            Combat.Initialise(playerReader, requirementFactory, logger);
            Adhoc.Initialise(playerReader, requirementFactory, logger);
            NPC.Initialise(playerReader, requirementFactory, logger);
            Parallel.Initialise(playerReader, requirementFactory, logger);
            ShapeshiftForm.ForEach(i => i.Initialise(playerReader, requirementFactory, logger));

            Jump.Key = JumpKey;
            Jump.Initialise(playerReader, requirementFactory, logger);

            Interact.Key = InteractKey;
            Interact.Initialise(playerReader, requirementFactory, logger);

            TargetLastTarget.Key = TargetLastTargetKey;
            TargetLastTarget.Initialise(playerReader, requirementFactory, logger);

            StandUp.Key = StandUpKey;
            StandUp.Initialise(playerReader, requirementFactory, logger);

            ClearTarget.Key = ClearTargetKey;
            ClearTarget.Initialise(playerReader, requirementFactory, logger);

            StopAttack.Key = StopAttackKey;
            StopAttack.Initialise(playerReader, requirementFactory, logger);

            TargetNearestTarget.Key = TargetNearestTargetKey;
            TargetNearestTarget.Initialise(playerReader, requirementFactory, logger);

            TargetPet.Key = TargetPetKey;
            TargetPet.Initialise(playerReader, requirementFactory, logger);

            TargetTargetOfTarget.Key = TargetTargetOfTargetKey;
            TargetTargetOfTarget.Initialise(playerReader, requirementFactory, logger);

            PetAttack.Key = PetAttackKey;
            PetAttack.Initialise(playerReader, requirementFactory, logger);

            Skinning.Key = SkinningKey;
            Skinning.Initialise(playerReader, requirementFactory, logger);

            GatherFindKeys.ForEach(key =>
            {
                GatherFindKeyConfig.Add(new KeyAction { Key = key });
                GatherFindKeyConfig.Last().Initialise(playerReader, requirementFactory, logger);
            });

            OverridePathFilename = overridePathProfileFile;
            if (!string.IsNullOrEmpty(OverridePathFilename))
            {
                PathFilename = OverridePathFilename;
            }

            if (!File.Exists(Path.Join(dataConfig.Path, PathFilename)))
            {
                if (!string.IsNullOrEmpty(OverridePathFilename))
                    throw new Exception($"The `{OverridePathFilename}` path file does not exists!");
                else
                    throw new Exception($"The loaded class config contains not existing `{PathFilename}` path file!");
            }
        }
    }
}