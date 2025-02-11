using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using CG.GameLoopStateMachine.GameStates;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Modules.Shield;
using CG.Ship.Repair;
using CG.Ship.Shield;
using CG.Space;
using Client.Utils;
using Gameplay.CompositeWeapons;
using Gameplay.Defects;
using Gameplay.Power;
using Gameplay.Quests;
using HarmonyLib;
using ToolClasses;
using UI.AstralMap;
using VoidSaving.ReadWriteTools;

namespace VoidSaving.Patches
{
    [HarmonyPatch]
    internal class LoadSavePatches
    {
        //Sets seed at earliest point
        [HarmonyPatch(typeof(HubQuestManager), "StartQuest"), HarmonyPrefix]
        static void LoadShipGUID(HubQuestManager __instance, Quest quest)
        {
            if (!SaveHandler.LoadSavedData) return;

            __instance.SelectedShipGuid = SaveHandler.ActiveData.ShipLoadoutGUID;
            quest.QuestParameters.Seed = SaveHandler.ActiveData.Seed;
            PunSingleton<PhotonService>.Instance.SetCurrentRoomShip(__instance.SelectedShipGuid);
            if (SaveHandler.ActiveData.ProgressionDisabled)
                VoidManager.Progression.ProgressionHandler.DisableProgression(MyPluginInfo.PLUGIN_GUID);
        }

        //Loads ship from vanilla ship data save/load system
        [HarmonyPatch(typeof(GameSessionManager), "LoadGameSessionNetworkedAssets"), HarmonyPrefix]
        static void ShipLoadPatch(GameSessionManager __instance)
        {
            if (!SaveHandler.LoadSavedData) return;

            __instance.activeGameSession.ToLoadShipData = ShipLoadout.FromJObject(SaveHandler.ActiveData.ShipLoadout);
        }

        //loads various ship data at start.
        [HarmonyPatch(typeof(AbstractPlayerControlledShip), "Start"), HarmonyPostfix]
        static void PostShipLoadPatch(AbstractPlayerControlledShip __instance)
        {
            if (!SaveHandler.LoadSavedData) return;


            SaveGameData activeData = SaveHandler.ActiveData;

            __instance.hitPoints = activeData.ShipHealth;
            HullDamageController HDC = __instance.GetComponentInChildren<HullDamageController>();
            HDC.State.repairableHp = activeData.RepairableShipHealth;
            Helpers.LoadBreachStates(HDC, activeData.Breaches);

            Helpers.AddBlueprintsToFabricator(__instance, activeData.UnlockedBPs);
            Helpers.AddRelicsToShip(__instance, activeData.Relics);
            __instance.GetComponentInChildren<FabricatorModule>().CurrentTier = activeData.FabricatorTier;

            ProtectedPowerSystem powerSystem = (ProtectedPowerSystem)__instance.ShipsPowerSystem;
            if (activeData.ShipPowered) { powerSystem.PowerOn(); }
            Helpers.LoadBreakers(powerSystem, activeData.BreakerData);

            int InstalledModuleIndex = 0;
            foreach (CellModule module in __instance.CoreSystems)
            {
                if (activeData.ShipSystemPowerStates[InstalledModuleIndex]) module.TurnOn();
                InstalledModuleIndex++;
            }

            BuildSocketController bsc = __instance.GetComponent<BuildSocketController>();
            InstalledModuleIndex = 0;
            int WeaponBulletsModuleIndex = 0;
            int KPDBulletsModuleIndex = 0;
            int AutoMechanicSwitchIndex = 0;
            int lifeSupportSwitchIndex = 0;

            foreach (BuildSocket socket in bsc.Sockets)
            {
                if (socket.InstalledModule == null) continue;


                if (activeData.ModulePowerStates[InstalledModuleIndex]) socket.InstalledModule.TurnOn();

                if (socket.InstalledModule is CompositeWeaponModule weaponModule && weaponModule.InsideElementsCollection.Magazine is BulletMagazine magazine)
                {
                    magazine.ammoLoaded = activeData.WeaponBullets[WeaponBulletsModuleIndex].AmmoLoaded;
                    magazine.reservoirAmmoCount = activeData.WeaponBullets[WeaponBulletsModuleIndex].AmmoReservoir;
                    WeaponBulletsModuleIndex++;
                }
                else if (socket.InstalledModule is KineticPointDefenseModule KPDModule)
                {
                    KPDModule.AmmoCount = activeData.KPDBullets[KPDBulletsModuleIndex++];
                }
                else if (socket.InstalledModule is AutoMechanicModule autoMechanicModule)
                {
                    autoMechanicModule.TriSwitch.ForceChange(activeData.AutoMechanicSwitches[AutoMechanicSwitchIndex++]);
                }
                else if (socket.InstalledModule is LifeSupportModule lifeSupportModule)
                {
                    lifeSupportModule.TemperatureSwitch.ForceChange(activeData.LifeSupportModeSwitches[lifeSupportSwitchIndex++]);
                }

                InstalledModuleIndex++;
            }

            Helpers.LoadEnhancements(__instance, activeData.Enhancements);
            Helpers.LoadBoosterStates(__instance, activeData.BoosterStates);
            Helpers.LoadAtmosphereValues(__instance, activeData.AtmosphereValues, activeData.AtmosphereBufferValues);
            Helpers.LoadDoorStates(__instance, activeData.DoorStates);
            Helpers.LoadAirlockSafeties(__instance, activeData.AirlockSafeties);

            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.AbstractPlayerShipStart);
        }

        //Quest Loading orders:
        //
        //
        //GSMasterStartGame.CreateGameSequence
        //GameSessionManager.HostGameSession()
        //  GameSession.LoadQuest()
        //    QuestGenerator.Create()
        //      EndlessQuestGenerator.CreateEndlessQuest()
        //        EndlessQuest.GenerateStartingSection()
        //          EndlessQuest.GenerateNextSection()
        //
        //
        //Void Jump Spin Up OnEnter
        //  VoidJumpInterdictionChance calculated by original quest seed, interdiction counter, jump counter
        //Void Jump Spin Up OnExit
        //  ExitCurrentSector
        //    EndlessQuestManager.Sector exited => CompleteSector => Add completed sectors
        //  EnterVoid
        //    AstralMapControler.VoidEntered
        //VoidJumpTravellingStable OnEnter
        //  if will be unstable, random time til unstable
        //VoidJumpSpinningDown OnEnter
        //  VoidJumpSystem.EnterSector
        //    GameSessionManager.EnterSectorInternal
        //      If entered next section, call EndlessQuestManager.EndCurrentSection
        //GenerateNextSection
        //  PrepareSectionParameters
        //  GenerateSection
        //

        //Just before void jump - Cache per-jump data.
        [HarmonyPatch(typeof(VoidJumpSpinningUp), "OnEnter"), HarmonyPrefix]
        static void GetInterdictionChancesPatch(VoidJumpSpinningUp __instance)
        {
            if (GameSessionManager.ActiveSession?.ActiveQuest is not EndlessQuest quest) return;

            if (SaveHandler.LoadSavedData)
            {
                quest.CurrentInterdictionChance = SaveHandler.ActiveData.CurrentInterdictionChance;
                quest.JumpCounter = SaveHandler.ActiveData.JumpCounter;
                quest.InterdictionCounter = SaveHandler.ActiveData.InterdictionCounter;
            }
            else
            {
                SaveHandler.LatestData.CurrentInterdictionChance = quest.CurrentInterdictionChance;
                SaveHandler.LatestData.JumpCounter = quest.JumpCounter;
                SaveHandler.LatestData.InterdictionCounter = quest.InterdictionCounter;
            }
        }

        //Collect last sector ID.
        [HarmonyPatch(typeof(EndlessQuestManager), "SectorEntered"), HarmonyPrefix]
        static void SectorEnteredDataCollectionPatch(EndlessQuestManager __instance, GameSessionSector sector)
        {
            if(!SaveHandler.LoadSavedData)
            {
                SaveHandler.LatestData.LastSectorID = sector.Id;
            }
        }

        //Load into last sector.
        [HarmonyPatch(typeof(GameSessionManager), "LoadActiveSector"), HarmonyPrefix]
        static bool LoadCurrentSectionPatch(GameSessionManager __instance)
        {
            if (SaveHandler.LoadSavedData)
            {
                EndlessQuest quest = (EndlessQuest)__instance.activeGameSession.ActiveQuest;
                Helpers.LoadCurrentSection(quest, SaveHandler.ActiveData);
                GameSessionSectorManager.Instance.EnterSector(SaveHandler.ActiveData.LastSectorID);
                GameSessionSectorManager.Instance.SetDestinationSector(-1);
                SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.LoadCurrentSection);
                return false;
            }
            else return true;
        }

        //Section generation data loaded - Runs after entering a sector belonging to the next section.
        [HarmonyPatch(typeof(EndlessQuest), "GenerateNextSection"), HarmonyPrefix]
        static void SectionDataLoadPatch(EndlessQuest __instance)
        {
            BepinPlugin.Log.LogInfo("GNS called");

            if (SaveHandler.LoadSavedData)
            {
                //Load saved random and quest data
                __instance.context.NextSectionParameters.Seed = SaveHandler.ActiveData.ParametersSeed;

                __instance.context.ActiveSolarSystemIndex = SaveHandler.ActiveData.ActiveSolarSystemID;
                __instance.context.NextSectionParameters.SolarSystem = __instance.parameters.SolarSystems[SaveHandler.ActiveData.ActiveSolarSystemID];

                __instance.context.NextSolarSystemIndex = SaveHandler.ActiveData.NextSolarSystemID;
                __instance.context.NextSectionParameters.EnemyLevelRange.Min = SaveHandler.ActiveData.EnemyLevelRangeMin;
                __instance.context.NextSectionParameters.EnemyLevelRange.Max = SaveHandler.ActiveData.EnemyLevelRangeMax;
                __instance.context.SectorsUsedInSolarSystem = SaveHandler.ActiveData.SectorsUsedInSolarSystem;
                __instance.context.SectorsToUseInSolarSystem = SaveHandler.ActiveData.SectorsToUseInSolarSystem;
                __instance.context.SideObjectiveGuaranteeInterval = SaveHandler.ActiveData.SideObjectiveGuaranteeInterval;
                __instance.context.NextSectionParameters.SectionIndex = SaveHandler.ActiveData.NextSectionID;
                __instance.context.NextSectionParameters.NextSectorId = SaveHandler.ActiveData.NextSectorID;
                __instance.context.NextSectionParameters.MissionId = SaveHandler.ActiveData.NextMissionID;
                Helpers.LoadLastGeneratedSectors(__instance, SaveHandler.ActiveData.GenerationResultsUsedSectors);
                Helpers.LoadLastGeneratedMainObjectives(__instance, SaveHandler.ActiveData.GenerationResultsUsedObjectives);
                Helpers.LoadCompletedSectors(__instance, SaveHandler.ActiveData.CompletedSectors);
                Helpers.LoadCompletedSections(__instance, SaveHandler.ActiveData.CompletedSections);

                __instance.context.Random = SaveHandler.ActiveData.Random.DeepCopy();

                GameSessionTracker.Instance._statistics = SaveHandler.ActiveData.SessionStats;

                SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.QuestData);

                if (VoidManager.BepinPlugin.Bindings.IsDebugMode)
                {
                    BepinPlugin.Log.LogInfo("Reading used Sectors");
                    foreach (var sector in __instance.context.lastGenerationResults.UsedSectors)
                    {
                        if (sector != default)
                            BepinPlugin.Log.LogInfo(sector.DisplayName);
                    }
                    BepinPlugin.Log.LogInfo("Reading used objectives");
                    foreach (var objective in __instance.context.lastGenerationResults.UsedMainObjectiveDefinitions)
                    {
                        if (objective != default)
                            BepinPlugin.Log.LogInfo(objective.Filename);
                    }
                }
            }
            else if (!GameSessionManager.InHub)
            {
                //Capture current random and quest data for saving prior to generation of next section.
                SaveHandler.LatestData.ParametersSeed = __instance.Context.NextSectionParameters.Seed;
                SaveHandler.LatestData.ActiveSolarSystemID = __instance.context.ActiveSolarSystemIndex;
                SaveHandler.LatestData.NextSolarSystemID = __instance.context.NextSolarSystemIndex;
                SaveHandler.LatestData.EnemyLevelRangeMin = __instance.context.NextSectionParameters.EnemyLevelRange.Min;
                SaveHandler.LatestData.EnemyLevelRangeMax = __instance.context.NextSectionParameters.EnemyLevelRange.Max;
                SaveHandler.LatestData.SectorsUsedInSolarSystem = __instance.context.SectorsUsedInSolarSystem;
                SaveHandler.LatestData.SectorsToUseInSolarSystem = __instance.context.SectorsToUseInSolarSystem;
                SaveHandler.LatestData.SideObjectiveGuaranteeInterval = __instance.context.SideObjectiveGuaranteeInterval;
                SaveHandler.LatestData.NextSectionID = __instance.context.NextSectionParameters.SectionIndex;
                SaveHandler.LatestData.NextSectorID = __instance.context.NextSectionParameters.NextSectorId;
                SaveHandler.LatestData.NextMissionID = __instance.context.NextSectionParameters.MissionId;
                SaveHandler.LatestData.GenerationResultsUsedSectors = Helpers.GetLastGeneratedSectors(__instance);
                SaveHandler.LatestData.GenerationResultsUsedObjectives = Helpers.GetLastGeneratedMainObjectives(__instance);


                SaveHandler.LatestData.Random = __instance.Context.Random.DeepCopy();

                if (VoidManager.BepinPlugin.Bindings.IsDebugMode && __instance.context.lastGenerationResults.UsedSectors != null)
                {
                    BepinPlugin.Log.LogInfo("Reading used Sectors");
                    foreach (var sector in __instance.context.lastGenerationResults.UsedSectors)
                    {
                        if (sector != default)
                            BepinPlugin.Log.LogInfo(sector.DisplayName);
                    }
                    BepinPlugin.Log.LogInfo("Reading used objectives");
                    foreach (var objective in __instance.context.lastGenerationResults.UsedMainObjectiveDefinitions)
                    {
                        if (objective != default)
                            BepinPlugin.Log.LogInfo(objective.Filename);
                    }
                }
            }
        }

        //Load Alloy, biomass and sheilds post OnEnter (alloy assigned late in the target method, shield healths assigned in unordered start methods
        [HarmonyPatch(typeof(GSIngame), "OnEnter"), HarmonyPostfix]
        static void PostInGameLoadPatch()
        {
            if (!SaveHandler.LoadSavedData) return;

            GameSessionSuppliesManager.Instance.AlloyAmount = SaveHandler.ActiveData.Alloy;
            GameSessionSuppliesManager.Instance.BiomassAmount = SaveHandler.ActiveData.Biomass;

            AbstractPlayerControlledShip playerShip = ClientGame.Current.PlayerShip;

            ShieldSystem ShipShields = playerShip.GetComponent<ShieldSystem>();
            for (int i = 0; i < 4; i++)
            {
                ShipShields._shields[i].hitPoints = SaveHandler.ActiveData.ShieldHealths[i];
                ShipShields._shields[i].UpdateShieldState();
            }

            BuildSocketController bsc = playerShip.GetComponent<BuildSocketController>();
            int SheildModuleDirectionsIndex = 0;
            foreach (BuildSocket socket in bsc.Sockets)
            {
                if (socket.InstalledModule == null) continue;
                if (socket.InstalledModule is ShieldModule shieldModule)
                {
                    shieldModule.IsClockwise.ForceChange(SaveHandler.ActiveData.ShieldDirections[SheildModuleDirectionsIndex++]);
                    shieldModule.IsForward.ForceChange(SaveHandler.ActiveData.ShieldDirections[SheildModuleDirectionsIndex++]);
                    shieldModule.IsCounterClockwise.ForceChange(SaveHandler.ActiveData.ShieldDirections[SheildModuleDirectionsIndex++]);
                }
            }

            //Defects loaded post-start due to the DamageController gathering defectSystems via start methods.
            Helpers.LoadDefectStates(playerShip.GetComponent<PlayerShipDefectDamageController>(), SaveHandler.ActiveData.Defects);


            //Jump System loaded post-start due to start() race condition conflicts with astral map
            VoidJumpSystem jumpSystem = playerShip.GetComponent<VoidJumpSystem>();
            jumpSystem.DebugTransitionToExitVectorSetState();
            jumpSystem.DebugTransitionToRotatingState();
            jumpSystem.DebugTransitionToSpinningUpState();

            //forcing next state via debug method ignores interdiction instant unstable chance. Instead, we'll force SpinUp start time -3 seconds
            var spinUpState = (VoidJumpSpinningUp)jumpSystem.activeState;
            spinUpState.enterTimestamp -= 3000;

            //Load module state after jumping.
            Helpers.LoadVoidDriveModule(ClientGame.Current.PlayerShip, SaveHandler.ActiveData.JumpModule);

            //reload astral map.
            AstralMapController mapController = playerShip.GetComponentInChildren<AstralMapController>();
            CalledInit = true;
            mapController.StartCoroutine(mapController.Init());

            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.InGameLoad);
        }

        static bool CalledInit;

        [HarmonyPatch(typeof(AstralMapController), "InitSections"), HarmonyPrefix]
        static void InitSectionsPrefix(AstralMapController __instance)
        {
            if (!CalledInit) return;

            __instance._gameStart = true;
        }

        [HarmonyPatch(typeof(AstralMapController), "InitSections"), HarmonyPostfix]
        static void InitSectionsPostfix(AstralMapController __instance)
        {
            if (!CalledInit) return;

            __instance._gameStart = false;
        }
    }
}
