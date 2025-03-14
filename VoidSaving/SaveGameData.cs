using CG.Game.Scenarios;
using CG.Ship.Hull;
using CG.Ship.Modules;
using Gameplay.Atmosphere;
using Gameplay.Enhancements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using System.Collections.Generic;

namespace VoidSaving
{
    public class SaveGameData
    {
        public uint SaveDataVersion;

        public string FileName;


        //PeekData
        public string PeekInfo = string.Empty; // - Old/Obsolete

        public string ShipName = string.Empty;

        public int PeekJumpCounter;

        public double TimePlayed;

        public float HealthPercent;


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

        public ShipSocketData[] BuildSocketCarryables;

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

        public int JumpCounter;

        public int InterdictionCounter;

        public float CurrentInterdictionChance;

        public FullSectorData[] CompletedSectors;

        public GameSessionStatistics SessionStats;
    }

    public struct SectionData
    {
        public FullSectorData[] ObjectiveSectors;

        public FullSectorData InterdictionSector;

        public int SolarSystemIndex;

        public int SectionIndex;
    }

    public struct FullSectorData
    {
        public FullSectorData(GameSessionSector Sector)
        {
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Converting sector data");
            State = Sector.ObjectiveState;
            SectorID = Sector.Id;
        }

        public ObjectiveState State;

        public int SectorID;
    }

    public struct SimpleSectorData
    {
        public SimpleSectorData(int solarSystemIndex, GUIDUnion sectorContainerGUID)
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

    public struct ShipSocketData
    {
        public ShipSocketData(BuildSocket socket)
        {
            SocketID = socket.Index;
            ObjectGUID = socket.Payload.assetGuid;
            JData = socket.Payload.SerializeExtraData ? socket.Payload.ExtraJData?.ToString(Formatting.None) : string.Empty;
        }

        public int SocketID;

        public GUIDUnion ObjectGUID;

        public string JData;
    }
}
