using CG.Game.Scenarios;
using CG.Objects;
using CG.Ship.Repair;
using CG.Space;
using System;
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
            return playerShip.GetComponentInChildren<FabricatorTerminal>().Data.CraftingData.SessionUnlockedItems.ToArray() ?? null;
        }

        public static void AddRelicsToShip(AbstractPlayerControlledShip playerShip, GUIDUnion[] relicIDs)
        {
            RelicSocketController[] RSCs = playerShip.GetComponentsInChildren<RelicSocketController>();
            int ControlerCount = RSCs.Length;
            for (int i = 0; i < ControlerCount; i++)
            {
                if (relicIDs[i] == GUIDUnion.Empty()) { continue; }
                try
                {
                    CarryableObject carryable = SpawnUtils.SpawnCarryable(relicIDs[i], RSCs[i].transform.position, RSCs[i].transform.rotation) as CarryableObject;
                    RSCs[i].RelicSocket.TryInsertCarryable(carryable);
                }
                catch (Exception e)
                {
                    BepinPlugin.Log.LogError($"Failed to spawn relic {relicIDs[i]} in controller!\n" + e);
                }
            }
        }

        public static void ApplyBreachStatesToBreaches(List<HullBreach> breaches, BreachCondition[] breachConditions)
        {
            int breachCount = breaches.Count;
            for (int i = 0; i < breachCount; i++)
            {
                breaches[i].SetCondition(breachConditions[i]);
            }
        }

        public static void AddBlueprintsToFabricator(AbstractPlayerControlledShip playerShip, GUIDUnion[] BPGUIDs)
        {
            playerShip.GetComponentInChildren<FabricatorTerminal>().Data.CraftingData.SessionUnlockedItems.AddRange(BPGUIDs);
        }
    }
}
