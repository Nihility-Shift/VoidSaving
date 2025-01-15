using CG.Ship.Repair;
using Newtonsoft.Json.Linq;
using System;

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

        //Data about quest/session/sectors

        public int seed;

        public Random random;

        public int JumpCounter;

        public int InterdictionCounter;
    }
}
