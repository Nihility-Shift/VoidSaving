using CG.Client.Quests;
using CG.Client.Ship;
using CG.Game.Missions;
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

namespace VoidSaving.ReadWriteTools
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

        /*
        public static List<SimpleSectorData> GetLastGeneratedSectors(EndlessQuest quest)
        {
            if (quest.context.lastGenerationResults.UsedSectors == null) return new();

            List<SolarSystem> solarSystems = quest.parameters.SolarSystems;

            return quest.context.lastGenerationResults.UsedSectors.ConvertAll<SimpleSectorData>
                (
                    sector => new SimpleSectorData()
                    {
                        SolarSystemIndex = solarSystems.IndexOf(sector.ParentSolarSystem),
                        SectorContainerGUID = sector.ContainerGuid
                    }
                );
        }

        public static void LoadLastGeneratedSectors(EndlessQuest quest, List<SimpleSectorData> data)
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

        public static void LoadLastGeneratedMainObjectives(EndlessQuest quest, GUIDUnion[] data)
        {
            List<ObjectiveDataRef> usedObjectives = new List<ObjectiveDataRef>();
            for (int i = 0; i < data.Length; i++)
            {
                GUIDUnion current = data[i];
                ObjectiveDataRef foundRef = quest.parameters.MainObjectiveDefinitions.FirstOrDefault(objective => objective.AssetGuid == current);
                if (foundRef != default)
                {
                    usedObjectives.Add(foundRef);
                }
            }
            quest.context.lastGenerationResults.UsedMainObjectiveDefinitions = usedObjectives;
            return;
            //One liner hehe, but barely readable.
            //quest.context.lastGenerationResults.UsedMainObjectiveDefinitions = data.Select(objectiveGUID => quest.parameters.MainObjectiveDefinitions.FirstOrDefault(objective => objective.AssetGuid == objectiveGUID)).ToList();
        }*/


        public static FullSectorData[] GetSectorDatasFromList(List<GameSessionSector> sectors, List<SolarSystem> solarSystems)
        {
            int SectorCount = sectors.Count;
            FullSectorData[] sectorDatas = new FullSectorData[SectorCount];
            for (int i = 0; i < SectorCount; i++)
            {
                GameSessionSector sector = sectors[i];

                sectorDatas[i] = new FullSectorData(sector, solarSystems.IndexOf(sector.SectorAsset.ParentSolarSystem));
            }
            return sectorDatas.ToArray();
        }

        /*
        public static FullSectorData[] GetSectorDatasFromList(List<GameSessionSector> sectors, int solarSystem)
        {
            int SectorCount = sectors.Count;
            FullSectorData[] sectorDatas = new FullSectorData[SectorCount];
            for (int i = 0; i < SectorCount; i++)
            {
                GameSessionSector sector = sectors[i];

                sectorDatas[i] = new FullSectorData(sector, solarSystem);
            }
            return sectorDatas.ToArray();
        }

        public static List<GameSessionSector> LoadSectorsFromData(EndlessQuest quest, FullSectorData[] datas, bool LoadMissions = false)
        {
            int dataLength = datas.Length;
            List<GameSessionSector> sectors = new List<GameSessionSector>(dataLength);
            List<SolarSystem> solarSystems = quest.parameters.SolarSystems;

            List<SolarSystemBossOptions> BossOptionsList = quest.parameters.SolarSystemBosses.SolarSystemConfig;

            for (int i = 0; i < dataLength; i++)
            {
                if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Running sector data load loop");

                FullSectorData sectorData = datas[i];

                SolarSystem solarSystem = solarSystems[sectorData.SolarSystemIndex];
                Sector sectorInternal;
                BossObjectiveConfig bossConfig = new();

                if (sectorData.SectorContainerGUID == solarSystem.InterdictionSector.ContainerGuid)
                {
                    //Interdiction sector
                    sectorInternal = solarSystem.InterdictionSector;
                }
                else
                {
                    bossConfig = BossOptionsList[sectorData.SolarSystemIndex].BossOptions.FirstOrDefault(bossOption => bossOption.Objective.AssetGuid == sectorData.ObjectiveGUID);
                    if (bossConfig.Sector)
                    {
                        //Boss sector
                        sectorInternal = bossConfig.Sector;
                    }
                    else
                    {
                        //Main/Side objective sector
                        sectorInternal = solarSystem.Sectors.FirstOrDefault(sector => sector.ContainerGuid == sectorData.SectorContainerGUID);
                    }
                }
                if (sectorInternal == null)
                {
                    sectorInternal = solarSystem.StagingSector;
                }
                GameSessionSector sector = new GameSessionSector(sectorInternal, sectorData.SectorID);

                //load Objective if not default GUID
                if (sectorData.ObjectiveGUID != default)
                {
                    //All objective Types exist in data container.
                    Objective objective = new Objective(ObjectiveDataContainer.Instance.GetAssetDefById(sectorData.ObjectiveGUID).Asset);

                    objective.ObjectiveSectors = new List<GameSessionSector> { sector };
                    objective.PrimarySector = sector;
                    objective.OffWorldSector = quest.OffWorldSector;

                    //Create objectives for all but last sector (to avoid loading objectives for latest sector, which causes exceptions on load.)
                    if (LoadMissions && sector.Id != SaveHandler.ActiveData.LastSectorID)
                    {
                        objective.AddClassifiersData(objective.Asset.GetAllClassifiers());
                        List<Mission> list2 = MissionsLoader.CreateMissions(objective.Classifiers);
                        for (int j = 0; j < list2.Count; j++)
                        {
                            list2[j].Id = j + sectorData.MissionID;
                        }
                        objective.ObjectiveMissions.AddRange(list2);
                        QuestGeneratorUtils.CreateObjectiveLogic(objective);
                        foreach (IQuestLogic questLogic in objective.GetAllLogic())
                        {
                            questLogic.SetDynamicReferences(objective, false);
                        }
                    }
                    sector.SetObjective(objective, sectorData.IsMainObjective);
                    sector.SectorObjective.Objective.State = sectorData.State;
                    sector.Difficulty.DifficultyModifier = sectorData.Difficulty;
                }

                sectors.Add(sector);
            }

            return sectors;
        }


        public static SectionData[] GetCompletedSections(EndlessQuest quest)
        {
            List<GameSessionSection> sections = quest.context.CompletedSections;
            List<SolarSystem> solarSystems = quest.parameters.SolarSystems;


            int length = sections.Count;
            SectionData[] data = new SectionData[length];
            for (int i = 0; i < length; i++)
            {
                int SystemID = solarSystems.IndexOf(sections[i].SolarSystem);
                data[i].ObjectiveSectors = GetSectorDatasFromList(sections[i].ObjectiveSectors, SystemID);
                data[i].SolarSystemIndex = SystemID;
                data[i].SectionIndex = sections[i].SectionIndex;
            }
            return data;
        }

        public static void LoadCompletedSections(EndlessQuest quest, SectionData[] datas)
        {
            List<GameSessionSection> sections = new List<GameSessionSection>(datas.Length);
            List<SolarSystem> solarSystems = quest.parameters.SolarSystems;


            int length = datas.Length;
            for (int i = 0; i < length; i++)
            {
                GameSessionSection currentSection = new GameSessionSection();
                SectionData data = datas[i];

                currentSection.ObjectiveSectors = LoadSectorsFromData(quest, data.ObjectiveSectors);
                currentSection.SolarSystem = solarSystems[data.SolarSystemIndex];
                currentSection.SectionIndex = data.SectionIndex;

                sections.Add(currentSection);
            }

            //Assigns first completed section to current generated section
            sections[0] = quest.context.CurrentSection;

            quest.context.CompletedSections = sections;
        }*/

        public static FullSectorData[] GetCompletedSectorDatas(EndlessQuest quest)
        {
            List<GameSessionSector> sectors = quest.context.CompletedSectors.GetRange(0, quest.context.CompletedSectors.Count());
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Collecting data of {sectors.Count()} sectors");

            FullSectorData[] sectorDatas = GetSectorDatasFromList(sectors, quest.parameters.SolarSystems);

            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Collected data of {sectorDatas.Length} sectors");
            return sectorDatas;
        }

        /*
        public static void LoadCompletedSectors(EndlessQuest endlessQuest, FullSectorData[] sectorDatas)
        {
            int sectorDataLength = sectorDatas.Length;
            FullSectorData[] AdjustedSectorDatas = sectorDatas[0..(sectorDataLength - 1)];
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Loading data of {sectorDataLength} sectors");

            List<GameSessionSector> completedSectors = LoadSectorsFromData(endlessQuest, AdjustedSectorDatas);

            sectorDataLength = completedSectors.Count(); //subtract 1 to not load last sector (technically current sector at time of loading)
            List<SectorCompletionInfo> completedInfos = new List<SectorCompletionInfo>(sectorDataLength);

            for (int i = 0; i < sectorDataLength; i++)
            {
                GameSessionSector currentSector = completedSectors[i];

                SectorCompletionInfo completionInfo = new SectorCompletionInfo();
                {
                    switch (currentSector.ObjectiveState)
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
                }
                completionInfo.SectorDifficultyModifier = currentSector.Difficulty.DifficultyModifier;
                completedInfos.Add(completionInfo);
            }
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Loaded data of {completedSectors.Count()} sectors");
            endlessQuest.context.CompletedSectors = completedSectors;
            endlessQuest.context.CompletedSectorStatus = completedInfos;
        }


        public static SectionData GetCurrentSection(EndlessQuest quest)
        {
            SectionData sectionData = new SectionData();
            GameSessionSection currentSection = quest.context.CurrentSection;

            sectionData.SolarSystemIndex = quest.context.ActiveSolarSystemIndex;

            if (currentSection.InterdictionSector != null)
                sectionData.InterdictionSector = GetSectorDatasFromList([currentSection.InterdictionSector], sectionData.SolarSystemIndex)[0];
            else
                sectionData.InterdictionSector = new FullSectorData() { SectorID = -99 };

            sectionData.ObjectiveSectors = GetSectorDatasFromList(currentSection.ObjectiveSectors, sectionData.SolarSystemIndex);

            return sectionData;
        }

        public static void LoadCurrentSection(EndlessQuest quest, SaveGameData saveData)
        {
            SectionData data = saveData.CurrentSection;
            GameSessionSection currentSection = new();

            currentSection.SectionIndex = data.SectionIndex;

            if (data.InterdictionSector.SectorID != -99)
            {
                currentSection.InterdictionSector = LoadSectorsFromData(quest, [data.InterdictionSector], true)[0];
            }

            currentSection.ObjectiveSectors = LoadSectorsFromData(quest, data.ObjectiveSectors, true);

            currentSection.SolarSystem = quest.parameters.SolarSystems[data.SolarSystemIndex];


            //Assign section sectors to historically completed sectors (replace references so reference comparisons match)
            List<GameSessionSector> completedSectors = quest.context.CompletedSectors;
            int CompletedSectorsCount = completedSectors.Count();
            List<GameSessionSector> SectionSectors = currentSection.AllAvailableSectors;
            int length = SectionSectors.Count();
            for (int i = 0; i < length; i++)
            {
                int SectionSectorID = SectionSectors[i].Id;
                int DetectedSectorIndex = -1;
                for (int j = Math.Max(CompletedSectorsCount - 6, 0); j < CompletedSectorsCount; j++)
                {
                    if (completedSectors[j].Id == SectionSectorID)
                    {
                        DetectedSectorIndex = j;
                        break;
                    }
                }
                if (DetectedSectorIndex != -1)
                {
                    BepinPlugin.Log.LogInfo($"Detected sector at index {DetectedSectorIndex}");
                    completedSectors[DetectedSectorIndex] = SectionSectors[i];
                }
            }

            quest.context.CurrentSection = currentSection;
            EndlessQuestManager.Instance.OnSectionChange(new GameSessionSection(), null, currentSection);
        }*/
    }
}
