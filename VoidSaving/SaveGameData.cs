using CG.Game.Scenarios;
using CG.Ship.Modules;
using CG.Ship.Repair;
using Gameplay.Atmosphere;
using Gameplay.Enhancements;
using Newtonsoft.Json.Linq;
using Photon.Pun;
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

        public int FabricatorTier;

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

        public EnhancementData[] Enhancements; //Engine Trims, Enhancement panels.

        public WeaponBullets[] WeaponBullets;

        public CircuitBreakerData BreakerData;

        public VoidDriveModuleData JumpModule;

        public AtmosphereValues[] AtmosphereValues;

        public bool[] ShieldDirections;

        public float[] ShieldHealths;

        public float[] KPDBullets;

        //List is best, as I have no idea how many ammo/power containers will be read it must be read from 2 different methods. It may be possible to change though...
        public List<float> AmmoResourceValues = new List<float>();

        public List<float> PowerResourceValues = new List<float>();


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

        public GameSessionStatistics SessionStats;
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

    public struct WeaponBullets
    {
        public WeaponBullets(float ammoLoaded, float ammoReservoir)
        {
            AmmoLoaded = ammoLoaded;
            AmmoReservoir = ammoReservoir;
        }

        public float AmmoLoaded;

        public float AmmoReservoir;
    }

    public struct EnhancementData
    {
        public EnhancementData(Enhancement enhancement)
        {
            int currentTimestamp = PhotonNetwork.ServerTimestamp;
            state = enhancement.State;
            ActivationTimeStart = enhancement._activationStartTime - currentTimestamp;
            ActivationTimeEnd = enhancement._activationEndTime - currentTimestamp;
            CooldownTimeStart = enhancement._cooldownStartTime - currentTimestamp;
            CooldownTimeEnd = enhancement._cooldownEndTime - currentTimestamp;
            FailureTimeStart = enhancement._failureStartTime - currentTimestamp;
            FailureTimeEnd = enhancement._failureEndTime - currentTimestamp;
            LastGrade = enhancement._lastActivationGrade;
            LastDurationMult = enhancement._lastDurationMultiplier;
        }

        public EnhancementState state;

        public float ActivationTimeEnd;

        public float ActivationTimeStart;

        public float CooldownTimeEnd;

        public float CooldownTimeStart;

        public float FailureTimeEnd;

        public float FailureTimeStart;

        public float LastGrade;

        public float LastDurationMult;
    }

    public struct CircuitBreakerData
    {
        public bool[] breakers;

        public float currentTemperature;

        public float NextBreakTemperature;
    }

    public struct VoidDriveModuleData
    {
        public VoidDriveModuleData(VoidDriveModule module)
        {
            engineChargedStates = module.EngineChargedStates;
            JumpCharge = module.JumpCharge;
        }

        public bool[] engineChargedStates;

        public float JumpCharge;
    }
}
