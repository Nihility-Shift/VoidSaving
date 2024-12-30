using CG.Ship.Repair;
using CG.Space;
using System.Collections.Generic;
using System.Linq;
using UI.Fabricator;

namespace VoidSaving
{
    internal class Helpers
    {
        public static BreachCondition[] BreachesAsConditionsArray(List<HullBreach> breaches)
        {
            return breaches.Select(breach => breach.State.condition).ToArray();
        }

        public static GUIDUnion[] RelicGUIDsFromShip(AbstractPlayerControlledShip playerShip)
        {
            RelicSocketController[] RSCs = playerShip.GetComponentsInChildren<RelicSocketController>();
            int ControlerCount = RSCs.Length;
            GUIDUnion[] Relics = new GUIDUnion[ControlerCount];
            for (int i = 0; i < ControlerCount; i++)
            {
                Relics[i] = RSCs[i].RelicSocket.Payload ? RSCs[i].RelicSocket.Payload.assetGuid : new GUIDUnion();
            }
            return Relics;
        }

        public static GUIDUnion[] UnlockedBPGUIDsFromShip(AbstractPlayerControlledShip playerShip)
        {
            return playerShip.GetComponent<FabricatorTerminal>().Data.CraftingData.SessionUnlockedItems.ToArray();
        }
    }
}
