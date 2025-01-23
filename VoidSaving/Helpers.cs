using CG.Game.Scenarios;
using CG.Objects;
using CG.Ship.Modules;
using CG.Ship.Repair;
using CG.Ship.Shield;
using CG.Space;
using Gameplay.Enhancements;
using Gameplay.Quests;
using Photon.Pun;
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

        public static BoosterStatus[] GetBoosterStates(AbstractPlayerControlledShip PlayerShip)
        {
            ThrusterBoosterController TBC = PlayerShip.GetComponent<ThrusterBoosterController>();
            int length = TBC.ThrusterBoosters.Count();
            BoosterStatus[] boosterstatuses = new BoosterStatus[length];
            for (int i = 0; i < length; i++)
            {
                ThrusterBooster currentBoster = TBC.ThrusterBoosters[i];
                boosterstatuses[i] = new BoosterStatus(currentBoster.State, currentBoster.DischargeTimer, currentBoster.ChargeTimer, currentBoster.CooldownTimer);
            }
            return boosterstatuses;
        }

        public static void LoadBoosterStates(AbstractPlayerControlledShip PlayerShip, BoosterStatus[] boosterStatuses)
        {
            ThrusterBoosterController TBC = PlayerShip.GetComponent<ThrusterBoosterController>();
            for (int i = 0; i < boosterStatuses.Length; i++)
            {
                ThrusterBooster currentBoster = TBC.ThrusterBoosters[i];
                currentBoster.ChangeState(boosterStatuses[i].BoosterState);
                currentBoster.ChargeTimer = boosterStatuses[i].ChargeTimer;
                currentBoster.CooldownTimer = boosterStatuses[i].CooldownTimer;
                currentBoster.DischargeTimer = boosterStatuses[i].DischargeTimer;
            }
        }

        public static float[] GetShipShieldHealths(AbstractPlayerControlledShip PlayerShip)
        {
            ShieldSystem shipShields = PlayerShip.GetComponent<ShieldSystem>();
            float[] shieldHealths = new float[4];
            for(int i = 0; i < 4; i++)
            {
                shieldHealths[i] = shipShields._shields[i].hitPoints;
            }
            return shieldHealths;
        }

        public static EnhancementData[] GetEnhancements(AbstractPlayerControlledShip PlayerShip)
        {
            Enhancement[] DetectedEhancements = PlayerShip.GetComponentsInChildren<Enhancement>();
            EnhancementData[] Trims = new EnhancementData[DetectedEhancements.Length];
            for (int i = 0; i < DetectedEhancements.Length; i++)
            {
                Trims[i] = new EnhancementData(DetectedEhancements[i]);
            }
            return Trims;
        }

        public static void LoadEnhancements(AbstractPlayerControlledShip PlayerShip, EnhancementData[] Trims)
        {
            Enhancement[] DetectedEhancements = PlayerShip.GetComponentsInChildren<Enhancement>();
            for (int i = 0; i < DetectedEhancements.Length; i++)
            {
                Enhancement enhancement = DetectedEhancements[i];
                EnhancementData data = Trims[i];
                enhancement.SetState(data.state, data.LastGrade, data.LastDurationMult, false);
                enhancement._activationStartTime = PhotonNetwork.ServerTimestamp + (int)data.ActivationTimeStart;
                enhancement._activationEndTime = PhotonNetwork.ServerTimestamp + (int)data.ActivationTimeEnd;
                enhancement._cooldownStartTime = PhotonNetwork.ServerTimestamp + (int)data.CooldownTimeStart;
                enhancement._cooldownEndTime = PhotonNetwork.ServerTimestamp + (int)data.CooldownTimeEnd;
                enhancement._failureStartTime = PhotonNetwork.ServerTimestamp + (int)data.FailureTimeStart;
                enhancement._failureEndTime = PhotonNetwork.ServerTimestamp + (int)data.FailureTimeEnd;
            }
        }
    }
}
