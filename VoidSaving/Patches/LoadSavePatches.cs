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
        //      EndlessQuestManager.EndCurrentSection
        //GenerateNextSection
        //  PrepareSectionParameters
        //  GenerateSection
        //

        //Section generation data loaded
        [HarmonyPatch(typeof(EndlessQuest), "GenerateNextSection"), HarmonyPrefix]
        static void SectionDataLoadPatch(EndlessQuest __instance)
        {
            BepinPlugin.Log.LogInfo("GNS called");

            if (SaveHandler.LoadSavedData)
            {
                __instance.CurrentInterdictionChance = SaveHandler.ActiveData.CurrentInterdictionChance;
                __instance.JumpCounter = SaveHandler.ActiveData.JumpCounter;
                __instance.InterdictionCounter = SaveHandler.ActiveData.InterdictionCounter;

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
                Helpers.LoadLastGeneratedSectors(__instance, SaveHandler.ActiveData.GenerationResultsUsedSectors);
                Helpers.LoadLastGeneratedMainObjectives(__instance, SaveHandler.ActiveData.GenerationResultsUsedObjectives);
                Helpers.LoadCompletedSectors(__instance, SaveHandler.ActiveData.CompletedSectors);
                Helpers.LoadCompletedSections(__instance, SaveHandler.ActiveData.CompletedSections);
                Helpers.LoadCurrentSection(__instance, SaveHandler.ActiveData.CurrentSection, __instance.CurrentInterdictionChance == __instance.Asset.InterdictionChanceOnReset);

                __instance.context.Random = SaveHandler.ActiveData.Random.DeepCopy();

                GameSessionTracker.Instance._statistics = SaveHandler.ActiveData.SessionStats;

                SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.QuestData);

                if (VoidManager.BepinPlugin.Bindings.IsDebugMode)
                {
                    BepinPlugin.Log.LogInfo("Reading used Sectors");
                    foreach (var sector in __instance.context.lastGenerationResults.UsedSectors)
                    {
                        BepinPlugin.Log.LogInfo(sector.DisplayName);
                    }
                    BepinPlugin.Log.LogInfo("Reading used objectives");
                    foreach (var objective in __instance.context.lastGenerationResults.UsedMainObjectiveDefinitions)
                    {
                        BepinPlugin.Log.LogInfo(objective.Filename);
                    }
            }
            }
            else if (!GameSessionManager.InHub)
            {
                SaveHandler.LatestData.CurrentInterdictionChance = __instance.CurrentInterdictionChance;
                SaveHandler.LatestData.JumpCounter = __instance.JumpCounter;
                SaveHandler.LatestData.InterdictionCounter = __instance.InterdictionCounter;
                if (VoidManager.BepinPlugin.Bindings.IsDebugMode)
                    BepinPlugin.Log.LogInfo($"Captured Interdiction at {__instance.CurrentInterdictionChance}");

                //Capture current random and quest data for saving prior to generation of next section.
                SaveHandler.LatestData.ParametersSeed = __instance.Context.NextSectionParameters.Seed;
                SaveHandler.LatestData.ActiveSolarSystemID = __instance.context.ActiveSolarSystemIndex;
                SaveHandler.LatestData.NextSolarSystemID = __instance.context.NextSolarSystemIndex;
                SaveHandler.LatestData.EnemyLevelRangeMin = __instance.context.NextSectionParameters.EnemyLevelRange.Min;
                SaveHandler.LatestData.EnemyLevelRangeMax = __instance.context.NextSectionParameters.EnemyLevelRange.Max;
                SaveHandler.LatestData.SectorsUsedInSolarSystem = __instance.context.SectorsUsedInSolarSystem;
                SaveHandler.LatestData.SectorsToUseInSolarSystem = __instance.context.SectorsToUseInSolarSystem;
                SaveHandler.LatestData.SideObjectiveGuaranteeInterval = __instance.context.SideObjectiveGuaranteeInterval;
                SaveHandler.LatestData.GenerationResultsUsedSectors = Helpers.GetLastGeneratedSectors(__instance);
                SaveHandler.LatestData.GenerationResultsUsedObjectives = Helpers.GetLastGeneratedMainObjectives(__instance);


                SaveHandler.LatestData.Random = __instance.Context.Random.DeepCopy();

                if (VoidManager.BepinPlugin.Bindings.IsDebugMode && __instance.context.lastGenerationResults.UsedSectors != null)
                {
                    BepinPlugin.Log.LogInfo("Reading used Sectors");
                    foreach (var sector in __instance.context.lastGenerationResults.UsedSectors)
                    {
                        BepinPlugin.Log.LogInfo(sector.DisplayName);
                    }
                    BepinPlugin.Log.LogInfo("Reading used objectives");
                    foreach (var objective in __instance.context.lastGenerationResults.UsedMainObjectiveDefinitions)
                    {
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
            jumpSystem.DebugTransitionToTravellingState();

            //Load module state after jumping.
            Helpers.LoadVoidDriveModule(ClientGame.Current.PlayerShip, SaveHandler.ActiveData.JumpModule);

            //reload astral map.
            //AstralMapController mapController = playerShip.GetComponentInChildren<AstralMapController>();
            //mapController.StartCoroutine(mapController.Init());

            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.VoidJumpStart);
            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.InGameLoad);
        }


        /*//Skips adding of current sector to completed sectors
        internal static bool SkipSectorComplete;

        static void SkipSectorCompletePatchMethod(EndlessQuestManager manager, GameSessionSector gameSessionSector, int index, byte status)
        {
            if (!SkipSectorComplete)
            {
                manager.endlessQuest.Context.CompletedSectors.Add(manager.endlessQuest.GetSector(index, true));
                manager.endlessQuest.Context.CompletedSectorStatus.Add(new SectorCompletionInfo
                {
                    CompletionStatus = (SectorCompletionStatus)status,
                    SectorDifficultyModifier = gameSessionSector.Difficulty.DifficultyModifier
                });
            }
        }

        static IEnumerable<CodeInstruction> SkipSectorCompletePatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] targetSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Newobj),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Stfld),
                new CodeInstruction(OpCodes.Callvirt),
            };

            CodeInstruction[] patchSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
            };

            return PatchBySequence(instructions, targetSequence, patchSequence, PatchMode.REPLACE, CheckMode.NEVER);
        }*/
    }
}
