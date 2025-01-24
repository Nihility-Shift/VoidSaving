using CG.Game.SpaceObjects.Controllers;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Modules.Shield;
using CG.Ship.Shield;
using CG.Space;
using Client.Utils;
using Gameplay.CompositeWeapons;
using Gameplay.Defects;
using Gameplay.Power;
using Gameplay.Quests;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch]
    internal class LoadingPatches
    {
        [HarmonyPatch(typeof(Quest), "NextJumpInterdictionCheck"), HarmonyPostfix]
        static void NJISeedCheck(int seed)
        {
            BepinPlugin.Log.LogInfo($"NJI Seed check: " + seed);
        }

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

                SaveHandler.LatestData.Random = activeData.Random;
                __instance.context.Random = SaveHandler.ActiveData.Random.DeepCopy();

                __instance.context.CompletedSectors = Helpers.LoadCompletedSectors(__instance, activeData.CompletedSectors);
                __instance.context.CompletedSectorStatus = Helpers.LoadCompletedSectorStatus(activeData.CompletedSectors);

                GameSessionTracker.Instance._statistics = activeData.SessionStats;
                SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.QuestData);
            }
            else
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


                SaveHandler.LatestData.Random = __instance.Context.Random.DeepCopy();
            }
        }

        //Sets seed at earliest point
        [HarmonyPatch(typeof(GameSession), "LoadQuest"), HarmonyPrefix]
        static void QuestLoadPrefix(GameSession __instance)
        {
            if (!SaveHandler.LoadSavedData || __instance.SessionQuestParameters == null) return;

            __instance.SessionQuestParameters.Seed = SaveHandler.ActiveData.Seed;
        }

        //Sets jump and interdiction counters prior to first usage
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
            PlayerShipDefectDamageController PSDDC = __instance.GetComponent<PlayerShipDefectDamageController>();
            PSDDC._hullDamageController.State.repairableHp = activeData.RepairableShipHealth;
            Helpers.LoadBreachStates(PSDDC._hullDamageController, activeData.Breaches);
            Helpers.LoadDefectStates(PSDDC, activeData.Defects);

            Helpers.AddBlueprintsToFabricator(__instance, activeData.UnlockedBPs);
            Helpers.AddRelicsToShip(__instance, activeData.Relics);
            __instance.GetComponent<FabricatorModule>().CurrentTier = activeData.FabricatorTier;

            ProtectedPowerSystem powerSystem = (ProtectedPowerSystem)__instance.ShipsPowerSystem;
            if (activeData.ShipPowered) { powerSystem.PowerOn(); }
            Helpers.LoadBreakers(powerSystem, activeData.BreakerData);

            int InstalledModuleIndex = 0;
            foreach (CellModule module in __instance.CoreSystems)
            {
                if (module != null && module.PowerDrain != null)
                {
                    if (activeData.ShipSystemPowerStates[InstalledModuleIndex])
                    {
                        module.TurnOn();
                    }
                    InstalledModuleIndex++;
                }
            }

            BuildSocketController bsc = __instance.GetComponent<BuildSocketController>();
            InstalledModuleIndex = 0;
            int WeaponBulletsModuleIndex = 0;
            int KPDBulletsModuleIndex = 0;
            int SheildModuleDirectionsIndex = 0;
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
                else if (socket.InstalledModule is ShieldModule shieldModule)
                {
                    shieldModule.IsClockwise.ForceChange(activeData.ShieldDirections[SheildModuleDirectionsIndex++]);
                    shieldModule.IsForward.ForceChange(activeData.ShieldDirections[SheildModuleDirectionsIndex++]);
                    shieldModule.IsCounterClockwise.ForceChange(activeData.ShieldDirections[SheildModuleDirectionsIndex++]);
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
            Helpers.LoadVoidDriveModule(__instance, activeData.JumpModule);
            Helpers.LoadAtmosphereValues(__instance, activeData.AtmosphereValues);
            Helpers.LoadDoorStates(__instance, activeData.DoorStates);
            Helpers.LoadAirlockSafeties(__instance, activeData.AirlockSafeties);

            GameSessionSuppliesManager.Instance.AlloyAmount = activeData.Alloy;
            GameSessionSuppliesManager.Instance.BiomassAmount = activeData.Biomass;

            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.AbstractPlayerShipStart);
        }

        //VJS start puts VJ into inactive. Put into travelling state after load.
        [HarmonyPatch(typeof(VoidJumpSystem), "Start"), HarmonyPostfix]
        static void PostVoidJumpSystemStartPatch(VoidJumpSystem __instance)
        {
            if (!SaveHandler.LoadSavedData) return;

            __instance.DebugTransitionToExitVectorSetState();
            __instance.DebugTransitionToRotatingState();
            __instance.DebugTransitionToSpinningUpState();
            __instance.DebugTransitionToTravellingState();
            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.VoidJumpStart);
        }


        //Loads shield hitpoints after all shields loaded.
        [HarmonyPatch(typeof(ShieldSystem), "AddShield"), HarmonyPostfix]
        static void LoadShieldHealthPatch(ShieldSystem __instance)
        {
            if (!SaveHandler.LoadSavedData) return;


            if(__instance._shields.Count == 4)
            {
                for(int i = 0; i < 4; i++)
                {
                    __instance._shields[i].hitPoints = SaveHandler.ActiveData.ShieldHealths[i];
                }
            }
        }
    }
}
