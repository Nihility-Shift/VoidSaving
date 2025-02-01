using CG.Game.Scenarios;
using CG.Ship.Modules;
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
        public uint SaveDataVersion;

        public string FileName;

        public string PeekInfo = string.Empty;

        public bool IronManMode = Config.DefaultIronMan.Value;

        public bool ProgressionDisabled;

        //Data about resources
        public int Alloy;

        public int Biomass;


        //Data about ship
        public GUIDUnion[] Relics;

        public GUIDUnion[] UnlockedBPs;

        public int FabricatorTier;

        public float ShipHealth;

        public float RepairableShipHealth;

        public byte[] Breaches;

        public sbyte[] Defects;


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

        public AtmosphereValues[] AtmosphereBufferValues;

        public bool[] DoorStates;

        public bool[] AirlockSafeties;

        public byte[] LifeSupportModeSwitches;

        public byte[] AutoMechanicSwitches;

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

        public List<SectorData> GenerationResultsUsedSectors;

        public GUIDUnion[] GenerationResultsUsedObjectives;

        public CompletedSectorData[] CompletedSectors;

        public GameSessionStatistics SessionStats;
    }

    public struct CompletedSectorData
    {
        public CompletedSectorData(GameSessionSector Sector, int solarSystemIndex)
        {
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Converting sector data");
            SolarSystemIndex = solarSystemIndex;
            SectorContainerGUID = Sector.SectorAsset.ContainerGuid;
            ObjectiveGUID = Sector.SectorObjective.Objective.Asset.ContainerGuid;
            Difficulty = Sector.Difficulty.DifficultyModifier;
            State = Sector.ObjectiveState;
        }

        //Save/Load as Byte.
        public int SolarSystemIndex;

        public GUIDUnion SectorContainerGUID;

        public GUIDUnion ObjectiveGUID;

        public DifficultyModifier Difficulty;

        public ObjectiveState State;
    }

    public struct SectorData
    {
        public SectorData(int solarSystemIndex, GUIDUnion sectorContainerGUID)
        {
            SolarSystemIndex = solarSystemIndex;
            SectorContainerGUID = sectorContainerGUID;
        }

        public int SolarSystemIndex;

        public GUIDUnion SectorContainerGUID;
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
        public EnhancementData(Enhancement enhancement, int moduleID)
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
            ParentModuleID = (short)moduleID;
        }

        public EnhancementState state;

        public int ActivationTimeEnd;

        public int ActivationTimeStart;

        public int CooldownTimeEnd;

        public int CooldownTimeStart;

        public int FailureTimeEnd;

        public int FailureTimeStart;

        public short ParentModuleID;

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
