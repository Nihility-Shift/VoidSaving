﻿using CG.Game.Scenarios;
using CG.Ship.Modules;
using CG.Ship.Repair;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace VoidSaving
{
    public class SaveGameData
    {
        public string FileName;

        public uint SaveDataVersion = 0;

        //Data about resources

        public int Alloy;

        public int Biomass;


        //Data about ship

        public GUIDUnion[] Relics;

        public GUIDUnion[] UnlockedBPs;

        public float ShipHealth;

        public float RepairableShipHealth;

        public BreachCondition[] Breaches;

        //Contains ShipType, Modules, loose carryables.
        public GUIDUnion ShipLoadoutGUID;

        public JObject ShipLoadout;

        public bool ShipPowered;

        public bool[] ModulePowerStates;

        public bool[] ShipSystemPowerStates;

        public BoosterStatus[] BoosterStates;

        //Data about quest/session/sectors

        public int Seed;

        public int ParametersSeed;

        public Random Random;

        public int JumpCounter;

        public int InterdictionCounter;

        public float CurrentInterdictionChance;

        public int NextSectorID;

        public int ActiveSolarSystemID;

        public int NextSolarSystemID;

        public int NextSectionIndex;

        public int EnemyLevelRangeMin;

        public int EnemyLevelRangeMax;

        public int SectorsUsedInSolarSystem;

        public int SectorsToUseInSolarSystem;

        public int SideObjectiveGuaranteeInterval;

        public SectorData[] CompletedSectors;
    }

    public struct SectorData
    {
        public GUIDUnion ObjectiveGUID;

        public DifficultyModifier Difficulty;

        public ObjectiveState State;

        public SectorData(GUIDUnion objectiveGUID, DifficultyModifier difficulty, ObjectiveState state)
        {
            ObjectiveGUID = objectiveGUID;
            Difficulty = difficulty;
            State = state;
        }

        public SectorData() { }
    }

    public struct BoosterStatus
    {
        public BoosterStatus(ThrusterBoosterState state, float dischargeTimer, float chargeTimer, float cooldownTimer)
        {
            BoosterState = state;
            DischargeTimer = dischargeTimer;
            ChargeTimer = chargeTimer;
            CooldownTimer = cooldownTimer;
        }

        public ThrusterBoosterState BoosterState;

        public float DischargeTimer;

        public float ChargeTimer;

        public float CooldownTimer;
    }
}
