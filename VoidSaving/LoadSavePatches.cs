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

namespace VoidSaving
{
    [HarmonyPatch]
    internal class LoadSavePatches
    {
        //Sets seed at earliest point
        [HarmonyPatch(typeof(VoidJumpSpinningUp), "OnEnter")]
        internal class CapturePreJumpPatch
        {
            static void Prefix()
            {
                if (GameSessionManager.ActiveSession.ActiveQuest is not EndlessQuest endlessQuest) return;

                if (!SaveHandler.LoadSavedData)
                {
                    SaveHandler.LatestData.CurrentInterdictionChance = endlessQuest.CurrentInterdictionChance;
                }
                else
                {
                    endlessQuest.CurrentInterdictionChance = SaveHandler.ActiveData.CurrentInterdictionChance;
                }
            }
        }

        [HarmonyPatch(typeof(EndlessQuest), "GenerateNextSection"), HarmonyPrefix]
        static void SectionDataLoadPatch(EndlessQuest __instance)
        {
            BepinPlugin.Log.LogInfo("GNS called");

            if (SaveHandler.LoadSavedData)
            {
                SaveGameData activeData = SaveHandler.ActiveData;

                __instance.context.NextSectionParameters.Seed = activeData.ParametersSeed;
                __instance.context.NextSectionParameters.NextSectorId = activeData.NextSectorID;

                __instance.context.ActiveSolarSystemIndex = activeData.ActiveSolarSystemID;
                __instance.context.NextSectionParameters.SolarSystem = __instance.parameters.SolarSystems[activeData.ActiveSolarSystemID];

                __instance.context.NextSolarSystemIndex = activeData.NextSolarSystemID;
                __instance.context.NextSectionParameters.SectionIndex = activeData.NextSectionIndex;
                __instance.context.NextSectionParameters.EnemyLevelRange.Min = activeData.EnemyLevelRangeMin;
                __instance.context.NextSectionParameters.EnemyLevelRange.Max = activeData.EnemyLevelRangeMax;
                __instance.context.SectorsUsedInSolarSystem = activeData.SectorsUsedInSolarSystem;
                __instance.context.SectorsToUseInSolarSystem = activeData.SectorsToUseInSolarSystem;

                __instance.JumpCounter = activeData.JumpCounter;
                __instance.InterdictionCounter = activeData.InterdictionCounter;


                SaveHandler.LatestData.Random = activeData.Random;
                __instance.context.Random = activeData.Random.DeepCopy();

                GameSessionTracker.Instance._statistics = activeData.SessionStats;
                SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.QuestData);
            }
            else if (!GameSessionManager.InHub)
            {
                //Capture current random and quest data for saving prior to generation of next section.
                SaveHandler.LatestData.ParametersSeed = __instance.Context.NextSectionParameters.Seed;
                SaveHandler.LatestData.NextSectorID = __instance.context.NextSectionParameters.NextSectorId;
                SaveHandler.LatestData.ActiveSolarSystemID = __instance.context.ActiveSolarSystemIndex;
                SaveHandler.LatestData.NextSolarSystemID = __instance.context.NextSolarSystemIndex;
                SaveHandler.LatestData.NextSectionIndex = __instance.context.NextSectionParameters.SectionIndex;
                SaveHandler.LatestData.EnemyLevelRangeMin = __instance.context.NextSectionParameters.EnemyLevelRange.Min;
                SaveHandler.LatestData.EnemyLevelRangeMax = __instance.context.NextSectionParameters.EnemyLevelRange.Max;
                SaveHandler.LatestData.SectorsUsedInSolarSystem = __instance.context.SectorsUsedInSolarSystem;
                SaveHandler.LatestData.SectorsToUseInSolarSystem = __instance.context.SectorsToUseInSolarSystem;

                SaveHandler.LatestData.JumpCounter = __instance.JumpCounter;
                SaveHandler.LatestData.InterdictionCounter = __instance.InterdictionCounter;


                SaveHandler.LatestData.Random = __instance.Context.Random.DeepCopy();
            }
        }

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

        //Sets jump and interdiction counters prior to first use, in this case on quest generation.
        [HarmonyPatch(typeof(QuestGenerator), "Create"), HarmonyPostfix]
        static void QuestLoadPostfix(Quest __result)
        {
            if (!SaveHandler.LoadSavedData || __result is not EndlessQuest quest) return;

            //Load JumpCounter prior to jump to keep Interdiction chance the same.
            quest.JumpCounter = SaveHandler.ActiveData.JumpCounter - 1;
            quest.InterdictionCounter = SaveHandler.ActiveData.InterdictionCounter;
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

        //VJS start puts VJ into inactive. Put into travelling state after load.
        /*[HarmonyPatch(typeof(VoidJumpSystem), "Start"), HarmonyPostfix]
        static void PostVoidJumpSystemStartPatch(VoidJumpSystem __instance)
        {
            if (!SaveHandler.LoadSavedData) return;

            __instance.DebugTransitionToExitVectorSetState();
            __instance.DebugTransitionToRotatingState();
            __instance.DebugTransitionToSpinningUpState();
            __instance.DebugTransitionToTravellingState();


            //Load module state after jumping.
            Helpers.LoadVoidDriveModule(ClientGame.Current.PlayerShip, SaveHandler.ActiveData.JumpModule);
            Helpers.LoadCompletedSectors((EndlessQuest)GameSessionManager.ActiveSession.ActiveQuest, SaveHandler.ActiveData.CompletedSectors);
            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.VoidJumpStart);
        }*/


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

            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.QuestData);

            VoidJumpSystem jumpSystem = playerShip.GetComponent<VoidJumpSystem>();
            jumpSystem.DebugTransitionToExitVectorSetState();
            jumpSystem.DebugTransitionToRotatingState();
            jumpSystem.DebugTransitionToSpinningUpState();
            jumpSystem.DebugTransitionToTravellingState();


            //Load module state after jumping.
            Helpers.LoadVoidDriveModule(ClientGame.Current.PlayerShip, SaveHandler.ActiveData.JumpModule);
            //Load completed sectors after jumping.
            Helpers.LoadCompletedSectors((EndlessQuest)GameSessionManager.ActiveSession.ActiveQuest, SaveHandler.ActiveData.CompletedSectors);
            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.VoidJumpStart);


            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.InGameLoad);
        }
    }
}
