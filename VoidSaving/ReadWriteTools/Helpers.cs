using CG.Client.Ship;
using CG.Game.Scenarios;
using CG.Game.SpaceObjects.Controllers;
using CG.Objects;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Repair;
using CG.Ship.Shield;
using CG.Space;
using Client.Utils;
using Code.Gameplay;
using Gameplay.Atmosphere;
using Gameplay.Defects;
using Gameplay.Enhancements;
using Gameplay.Power;
using Gameplay.Quests;
using Photon.Pun;
using ResourceAssets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VoidSaving
{
    internal class Helpers
    {
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
            return playerShip.GetComponentInChildren<FabricatorModule>().SessionBasedUnlockPool.ToArray();
        }

        public static void AddBlueprintsToFabricator(AbstractPlayerControlledShip playerShip, GUIDUnion[] BPGUIDs)
        {
            foreach (GUIDUnion UnlockedItem in BPGUIDs)
            {
                playerShip.GetComponentInChildren<FabricatorModule>().TryAddItemToSharedUnlockPool(UnlockedItem, false);
            }
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

        public static List<SectorData> GetLastGenUsedSectors(EndlessQuest quest)
        {
            if (quest.context.lastGenerationResults.UsedSectors == null) return new();

            List<SolarSystem> solarSystems = quest.parameters.SolarSystems;

            return quest.context.lastGenerationResults.UsedSectors.ConvertAll<SectorData>
                (
                    sector => new SectorData() 
                    { 
                        SolarSystemIndex = solarSystems.IndexOf(sector.ParentSolarSystem), 
                        SectorContainerGUID = sector.ContainerGuid 
                    }
                );
        }

        public static void LoadLastGenUsedSectors(EndlessQuest quest, List<SectorData> data)
        {
            List<SolarSystem> solarSystems = quest.parameters.SolarSystems;

            List<Sector> sectors = data.ConvertAll(sectorData => solarSystems[sectorData.SolarSystemIndex].Sectors.FirstOrDefault(sector => sector.ContainerGuid == sectorData.SectorContainerGUID));

            quest.context.lastGenerationResults.UsedSectors = sectors;
        }


        public static GUIDUnion[] GetLastGeneratedMainObjectives(EndlessQuest quest)
        {
            if (quest.context.lastGenerationResults.UsedMainObjectiveDefinitions == null) return new GUIDUnion[0];

            return quest.context.lastGenerationResults.UsedMainObjectiveDefinitions.Select(objectiveDef => objectiveDef.AssetGuid).ToArray();
        }

        public static void LoadLastUsedMainObjectives(EndlessQuest quest, GUIDUnion[] data)
        {
            quest.context.lastGenerationResults.UsedMainObjectiveDefinitions = data.Select(objectiveGUID => new ObjectiveDataRef(objectiveGUID.AsIntArray())).ToList();
        }

        public static CompletedSectorData[] GetCompletedSectorDatas(EndlessQuest quest)
        {
            List<GameSessionSector> sectors = quest.context.CompletedSectors;
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Collecting data of {sectors.Count()} sectors");

            List<SolarSystem> solarSystems = quest.parameters.SolarSystems;

            List<CompletedSectorData> sectorDatas = new();
            foreach (GameSessionSector sector in sectors) //start at 1 to ignore starting sector
            {
                if (sector == quest.StartSector) continue;
                sectorDatas.Add( new CompletedSectorData(sector, solarSystems.IndexOf(sector.SectorAsset.ParentSolarSystem)));
            }

            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Collected data of {sectorDatas.Count} sectors");
            return sectorDatas.ToArray();
        }

        public static void LoadCompletedSectors(EndlessQuest endlessQuest, CompletedSectorData[] sectorDatas)
        {
            List<GameSessionSector> CompletedSectors = new List<GameSessionSector>(sectorDatas.Length);
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Loading data of {sectorDatas.Count()} sectors");

            List<SolarSystem> solarSystems = endlessQuest.parameters.SolarSystems;
            List<SolarSystemBossOptions> BossOptionsList = endlessQuest.parameters.SolarSystemBosses.SolarSystemConfig;

            foreach (CompletedSectorData sectorData in sectorDatas)
            {
                if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Running sector data load loop");
                if (sectorData.SectorContainerGUID == default) { BepinPlugin.Log.LogWarning("Detected default GUID on completed sector load."); continue; } //Skip null/empty GUIDs. Should never occur.

                try
                {
                    //Load Sector

                    //Attempt get boss config by checking objective GUIDs in all boss option lists.
                    BossObjectiveConfig bossConfig = BossOptionsList[sectorData.SolarSystemIndex].BossOptions.FirstOrDefault(bossOption => bossOption.Objective.AssetGuid == sectorData.ObjectiveGUID);

                    //Create sector from boss config if found, otherwise use detected value from sectors.
                    GameSessionSector sector = new GameSessionSector(bossConfig.Sector ?? solarSystems[sectorData.SolarSystemIndex].Sectors.First(sector => sector.ContainerGuid == sectorData.SectorContainerGUID));

                    //load Objective
                    sector.SetObjective(new Objective(ObjectiveDataContainer.Instance.GetAssetDefById(sectorData.ObjectiveGUID).Asset));
                    sector.SectorObjective.Objective.State = sectorData.State;
                    sector.Difficulty.DifficultyModifier = sectorData.Difficulty;
                    CompletedSectors.Add(sector);
                }
                catch (Exception ex)
                {
                    BepinPlugin.Log.LogError($"Failed to load a sector-objective pair.\n{ex}");
                }
            }
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Loaded data of {CompletedSectors.Count()} sectors");
            endlessQuest.context.CompletedSectors.AddRange(CompletedSectors);


            //Load Statuses
            List<SectorCompletionInfo> completedInfos = new List<SectorCompletionInfo>(sectorDatas.Length);
            foreach (CompletedSectorData sectorData in sectorDatas)
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
            endlessQuest.context.CompletedSectorStatus.AddRange(completedInfos);
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
            for (int i = 0; i < 4; i++)
            {
                shieldHealths[i] = shipShields._shields[i].hitPoints;
            }
            return shieldHealths;
        }

        public static EnhancementData[] GetEnhancements(AbstractPlayerControlledShip PlayerShip)
        {
            //Enhancements do not naturatlly load in the same order, and the best way I came up with to determine the same enhnacement is by basing on the parent module buildsocket index.
            //The build socket index isn't easily accessible, and so this method gathers that index for saving within the enhancement data.

            //Gather modules w/indexes
            BuildSocketController BSC = PlayerShip.GetComponent<BuildSocketController>();
            Dictionary<CellModule, int> ModuleIndexes = new();
            foreach (BuildSocket socket in BSC.Sockets)
            {
                if (socket.InstalledModule != null)
                {
                    ModuleIndexes.Add(socket.InstalledModule, socket.Index);
                }
            }

            //Convert detected enhancments to enhancement data w/detected indexes. If parent module is not found in gathered modules, module is asumed part of ship systems.
            Enhancement[] DetectedEhancements = PlayerShip.GetComponentsInChildren<Enhancement>();
            EnhancementData[] EnhancementDatas = new EnhancementData[DetectedEhancements.Length];
            for (int i = 0; i < DetectedEhancements.Length; i++)
            {
                CellModule module = DetectedEhancements[i].GetComponentInParent<CellModule>();

                if (module != null && ModuleIndexes.TryGetValue(module, out int index))
                {
                    EnhancementDatas[i] = new EnhancementData(DetectedEhancements[i], index);
                }
                else
                {
                    EnhancementDatas[i] = new EnhancementData(DetectedEhancements[i], -1);
                }

                if (VoidManager.BepinPlugin.Bindings.IsDebugMode)
                {
                    EnhancementData thingy = EnhancementDatas[i];
                    Enhancement thing = DetectedEhancements[i];
                    BepinPlugin.Log.LogInfo($"Collected Enhancement: {thing.contextInfo.HeaderText} {thing.name} of moduleIndex {thingy.ParentModuleID} with data: {thingy.state} Grade: {thingy.LastGrade} Duration: {thingy.LastDurationMult}x");
                }
            }
            return EnhancementDatas;
        }

        public static void LoadEnhancements(AbstractPlayerControlledShip PlayerShip, EnhancementData[] datas)
        {
            //Gather modules w/indexes
            BuildSocketController BSC = PlayerShip.GetComponent<BuildSocketController>();
            Dictionary<CellModule, int> ModuleIndexes = new();
            foreach (BuildSocket socket in BSC.Sockets)
            {
                if (socket.InstalledModule != null)
                {
                    ModuleIndexes.Add(socket.InstalledModule, socket.Index);
                }
            }

            //Add detected enhancements with indexes to list.
            Dictionary<Enhancement, int> EnhancementIndexes = new();
            Enhancement[] DetectedEhancements = PlayerShip.GetComponentsInChildren<Enhancement>();
            for (int i = 0; i < DetectedEhancements.Length; i++)
            {
                CellModule module = DetectedEhancements[i].GetComponentInParent<CellModule>();
                if (module != null && ModuleIndexes.TryGetValue(module, out int index))
                {
                    EnhancementIndexes.Add(DetectedEhancements[i], index);
                }
                else
                {
                    EnhancementIndexes.Add(DetectedEhancements[i], -1);
                }
            }

            //Load input datas
            foreach (EnhancementData data in datas)
            {
                //Find an enhancement with the same module index.
                Enhancement enhancement = null;
                foreach (KeyValuePair<Enhancement, int> enhance in EnhancementIndexes)
                {
                    if (data.ParentModuleID == enhance.Value)
                    {
                        enhancement = enhance.Key;
                        break;
                    }
                }
                if (enhancement == null)
                {
                    BepinPlugin.Log.LogWarning($"Failed to find enhancement for data of module index {data.ParentModuleID}");
                    continue;
                }
                else
                {
                    EnhancementIndexes.Remove(enhancement);
                }

                if (VoidManager.BepinPlugin.Bindings.IsDebugMode)
                    BepinPlugin.Log.LogInfo($"Loading Enhancement: {enhancement.contextInfo.HeaderText} {enhancement.name} of moduleIndex {data.ParentModuleID} with data: {data.state} Grade: {data.LastGrade} Duration: {data.LastDurationMult}x");

                try
                {
                    enhancement.SetState(data.state, data.LastGrade, data.LastDurationMult, false);
                    enhancement._activationStartTime = PhotonNetwork.ServerTimestamp + data.ActivationTimeStart;
                    enhancement._activationEndTime = PhotonNetwork.ServerTimestamp + data.ActivationTimeEnd;
                    enhancement._cooldownStartTime = PhotonNetwork.ServerTimestamp + data.CooldownTimeStart;
                    enhancement._cooldownEndTime = PhotonNetwork.ServerTimestamp + data.CooldownTimeEnd;
                    enhancement._failureStartTime = PhotonNetwork.ServerTimestamp + data.FailureTimeStart;
                    enhancement._failureEndTime = PhotonNetwork.ServerTimestamp + data.FailureTimeEnd;
                }
                catch (Exception ex)
                {
                    BepinPlugin.Log.LogError(ex);
                }
            }
        }


        public static CircuitBreakerData GetBreakerData(ProtectedPowerSystem powerSystem)
        {
            CircuitBreakerData data = new CircuitBreakerData();
            data.breakers = powerSystem.Breakers.Select(x => x.IsOn.Value).ToArray();
            data.currentTemperature = powerSystem.currentTemperature;
            data.NextBreakTemperature = powerSystem.NextBreakTemperature;
            return data;
        }

        public static void LoadBreakers(ProtectedPowerSystem powerSystem, CircuitBreakerData data)
        {
            for (int i = 0; i < data.breakers.Length; i++)
            {
                powerSystem.Breakers[i].IsOn.ForceChange(data.breakers[i]);
            }
            powerSystem.NextBreakTemperature = data.NextBreakTemperature;
            powerSystem.currentTemperature = data.currentTemperature;
        }


        public static void LoadVoidDriveModule(AbstractPlayerControlledShip playerShip, VoidDriveModuleData data)
        {
            VoidDriveModule drive = playerShip.GetComponentInChildren<VoidDriveModule>();

            for (int i = 0; i < data.engineChargedStates.Length; i++)
            {
                drive.SetEngineCharging(i, data.engineChargedStates[i], new PhotonMessageInfo(PhotonNetwork.LocalPlayer, PhotonNetwork.ServerTimestamp, null));
            }
            drive.JumpCharge = data.JumpCharge;
        }


        public static Tuple<AtmosphereValues[], AtmosphereValues[]> GetAtmosphereValues(AbstractPlayerControlledShip playerShip)
        {
            Atmosphere atmosphere = playerShip.GetComponentInChildren<Atmosphere>();
            return new Tuple<AtmosphereValues[], AtmosphereValues[]>(atmosphere.Atmospheres.elements.ToArray(), atmosphere._buffer.ToArray());
        }

        public static void LoadAtmosphereValues(AbstractPlayerControlledShip playerShip, AtmosphereValues[] data, AtmosphereValues[] bufferData)
        {
            Atmosphere atmosphere = playerShip.GetComponentInChildren<Atmosphere>();
            for (int i = 0; i < data.Count(); i++)
            {
                atmosphere.RoomAtmospheres.SetElementAt(i, data[i]);
            }
            atmosphere._buffer = bufferData.ToList();
        }


        public static bool[] GetDoorStates(AbstractPlayerControlledShip playerShip)
        {
            return playerShip.GetComponentsInChildren<AbstractDoor>().Select(door => door.isOpen).ToArray();
        }

        public static void LoadDoorStates(AbstractPlayerControlledShip playerShip, bool[] states)
        {
            AbstractDoor[] doors = playerShip.GetComponentsInChildren<AbstractDoor>();
            for (int i = 0; i < states.Length; i++)
            {
                doors[i].IsOpen = states[i];
            }
        }


        public static bool[] GetAirlockSafeties(AbstractPlayerControlledShip playerShip)
        {
            Airlock[] airlocks = playerShip.GetComponentsInChildren<Airlock>();
            bool[] states = new bool[airlocks.Length];
            for (int i = 0; i < airlocks.Length; i++)
            {
                states[i] = airlocks[i].IsSafetyEnabled;
            }
            return states;
        }

        public static void LoadAirlockSafeties(AbstractPlayerControlledShip playerShip, bool[] states)
        {
            Airlock[] airlocks = playerShip.GetComponentsInChildren<Airlock>();
            for (int i = 0; i < states.Length; i++)
            {
                airlocks[i].IsSafetyEnabled = states[i];
            }
        }


        public static byte[] GetBreachStates(HullDamageController damageController)
        {
            return damageController.Breaches.Select(breach => (byte)breach.State.condition).ToArray();
        }

        public static void LoadBreachStates(HullDamageController damageController, byte[] breachConditions)
        {
            int breachCount = breachConditions.Length;
            for (int i = 0; i < breachCount; i++)
            {
                damageController.breaches[i].SetCondition((BreachCondition)breachConditions[i]);
            }
        }


        public static sbyte[] GetDefectStates(PlayerShipDefectDamageController damageController)
        {
            List<sbyte> states = new List<sbyte>();
            foreach (DefectSystem system in damageController._defectSystems)
            {
                foreach (Defect defect in system.AvailableDefects)
                {
                    states.Add((sbyte)defect.activeStageIndex);
                }
            }
            return states.ToArray();
        }

        public static void LoadDefectStates(PlayerShipDefectDamageController damageController, sbyte[] states)
        {
            int length = states.Length;
            int currentDefectIndex = 0;
            foreach (DefectSystem defectSystem in damageController._defectSystems)
            {
                foreach (Defect defect in defectSystem.AvailableDefects)
                {
                    defect.SetDefectStage(states[currentDefectIndex++]);
                    if (currentDefectIndex == length)
                    {
                        return;
                    }
                }
            }
        }
    }
}
