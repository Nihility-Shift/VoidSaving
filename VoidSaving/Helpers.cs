using CG.Game.Scenarios;
using CG.Objects;
using CG.Ship.Modules;
using CG.Ship.Repair;
using CG.Space;
using Gameplay.Quests;
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
            foreach (GUIDUnion UnlockedItem in BPGUIDs)
            {
                playerShip.GetComponentInChildren<FabricatorModule>().TryAddItemToSharedUnlockPool(UnlockedItem, false);
            }
        }

        public static List<GameSessionSector> LoadCompletedSectors(EndlessQuest endlessQuest, SectorData[] sectorDatas)
        {
            List<GameSessionSector> CompletedSectors = new List<GameSessionSector>();

            foreach (SectorData sectorData in sectorDatas)
            {
                GameSessionSector sector = new GameSessionSector(new Code.Gameplay.Sector());
                GameSessionSectorObjective objective = new GameSessionSectorObjective();
                objective.Objective = new Objective(endlessQuest.context.NextSectionParameters.ViableMainObjectives.First(thing => thing.AssetGuid == sectorData.ObjectiveGUID).Asset);
                objective.Objective.State = sectorData.State;

                sector.SectorObjective = objective;
                sector.Difficulty.DifficultyModifier = sectorData.Difficulty;
                CompletedSectors.Add(sector);
            }
            return CompletedSectors;
        }

        public static SectorData[] GetCompletedSectorDatas(List<GameSessionSector> sectors)
        {
            SectorData[] sectorDatas = new SectorData[sectors.Count];
            for (int i = 0; i < sectors.Count; i++)
            {
                GameSessionSector sector = sectors[i];

                sectorDatas[i] = new SectorData(sector.SectorObjective.Objective.Asset.assetGuid, sector.Difficulty.DifficultyModifier, sector.ObjectiveState);
            }

            return sectorDatas;
        }

        public static List<SectorCompletionInfo> LoadCompletedSectorStatus(SectorData[] sectorDatas)
        {
            List<SectorCompletionInfo> completedInfos = new List<SectorCompletionInfo>(sectorDatas.Length);
            foreach (SectorData sectorData in sectorDatas)
            {
                SectorCompletionInfo completionInfo = new SectorCompletionInfo();
                switch (sectorData.State)
                {
                    case ObjectiveState.Inactive:
                    case ObjectiveState.Available:
                    case ObjectiveState.Started:
                    case ObjectiveState.Failed:
                        completionInfo.CompletionStatus = SectorCompletionStatus.Failed;
                        break;
                    case ObjectiveState.Completed:
                        completionInfo.CompletionStatus = SectorCompletionStatus.Completed;
                        break;
                    case ObjectiveState.NoObjective:
                        completionInfo.CompletionStatus = SectorCompletionStatus.NothingToDo;
                        break;
                }
                completionInfo.SectorDifficultyModifier = sectorData.Difficulty;

                completedInfos.Add(completionInfo);
            }

            return completedInfos;
        }
    }
}
