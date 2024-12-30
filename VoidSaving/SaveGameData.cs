using CG.Ship.Repair;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidSaving
{
    public class SaveGameData
    {
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
        public JObject ShipLoadout;

        //Data about quest/session/sectors

        public int seed;

        public Random random;

        public int JumpCounter;

        public int InterdictionCounter;
    }
}
