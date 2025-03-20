using CG.Game;
using CG.Game.Scenarios;
using CG.Game.SpaceObjects.Controllers;
using CG.GameLoopStateMachine.GameStates;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Modules.Shield;
using CG.Ship.Repair;
using CG.Ship.Shield;
using CG.Space;
using Gameplay.CompositeWeapons;
using Gameplay.Defects;
using Gameplay.Power;
using Gameplay.Quests;
using HarmonyLib;
using Photon.Pun;
using ToolClasses;
using VoidSaving.ReadWriteTools;
using VoidSaving.VanillaFixes;

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

            if (activeData.SaveDataVersion >= 1)
                Helpers.LoadBuildSocketPayloads(bsc, SaveHandler.ActiveData.BuildSocketCarryables);


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

        //Load into last sector.
        [HarmonyPatch(typeof(GameSessionManager), "LoadActiveSector"), HarmonyPrefix]
        static bool LoadCurrentSectionPatch(GameSessionManager __instance)
        {
            if (!SaveHandler.LoadSavedData) return true;


            //Load completed sectors before moving to initial sector.
            int CompletedDataCount = SaveHandler.ActiveData.CompletedSectors.Length;
            FullSectorData data = default;
            for (int i = 0; i < CompletedDataCount; i++)
            {
                data = SaveHandler.ActiveData.CompletedSectors[i];

                SectorCompletionStatus sectorCompletionStatus = (SectorCompletionStatus)0;
                switch (data.State)
                {
                    case ObjectiveState.Inactive:
                    case ObjectiveState.Available:
                    case ObjectiveState.Started:
                    case ObjectiveState.Failed:
                        sectorCompletionStatus = SectorCompletionStatus.Failed;
                        break;
                    case ObjectiveState.Completed:
                        sectorCompletionStatus = SectorCompletionStatus.Completed;
                        break;
                    case ObjectiveState.NoObjective:
                        sectorCompletionStatus = SectorCompletionStatus.NothingToDo;
                        break;
                }
                EndlessQuestManager.Instance.photonView.RPC("CompleteSector", RpcTarget.AllBuffered, new object[]
                {
                        data.SectorID,
                        (byte)sectorCompletionStatus,
                });
            }

            //Set objective state before entering the sector to allow skip code to function.
            GameSessionSector sector = GameSessionManager.ActiveSession.GetSectorById(data.SectorID, false);
            if (sector.SectorObjective != null)
                sector.SectorObjective.Objective.State = data.State;

            //Enter sector as initial sector, ensuring it appears on the astral map.
            SessionManagerPropertiesFix.BlockLoadingExecution = true;
            GameSessionSectorManager.Instance.EnterSector(data.SectorID);
            GameSessionSectorManager.Instance.SetDestinationSector(-1);
            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.SectorLoad);
            return false;
        }

        static bool FirstLoadJump;

        //Block Sector completion on first jump from null sector.
        [HarmonyPatch(typeof(EndlessQuestManager), "SectorExited"), HarmonyPrefix]
        static bool StopFirstSectorCompletionPatch()
        {
            if (FirstLoadJump)
            {
                FirstLoadJump = false;
                return false;
            }
            return true;
        }

        //Load Alloy, biomass and sheilds post OnEnter (alloy assigned late in the target method, shield healths assigned in unordered start methods
        [HarmonyPatch(typeof(GSIngame), "OnEnter"), HarmonyPostfix]
        static void PostInGameLoadPatch()
        {
            if (!SaveHandler.LoadSavedData) return;

            GameSessionSuppliesManager.Instance.SetAlloyAmountNoNotification(SaveHandler.ActiveData.Alloy, ResourceChangeAlloy.GAME_STATE_INGAME_ENTER);
            GameSessionSuppliesManager.Instance.SetBiomassAmountNoNotification(SaveHandler.ActiveData.Biomass, ResourceChangeBiomass.GAMESESSION_SETUP);

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

            //Starts in homunculus mode, despite homunculus having been dispensed by now.
            playerShip.GetComponentInChildren<HomunculusAndBiomassSocket>().SwitchToBiomassSocket();


            //Jump System loaded post-start due to start() race condition conflicts with astral map
            FirstLoadJump = true;
            AutoSavePatch.FirstJump = true;
            VoidJumpSystem jumpSystem = playerShip.GetComponent<VoidJumpSystem>();
            jumpSystem.DebugTransitionToExitVectorSetState();
            jumpSystem.DebugTransitionToRotatingState();
            jumpSystem.DebugTransitionToSpinningUpState();
            BlockSpinUpSignalResolution = true; // UpdateState sends the discharge signal, canceling/breaking void jump.

            //forcing next state via debug method ignores interdiction instant unstable chance. Instead, we'll force SpinUp start time -3 seconds
            var spinUpState = (VoidJumpSpinningUp)jumpSystem.activeState;
            spinUpState.enterTimestamp -= 3000;

            //Load module state after jumping.
            Helpers.LoadVoidDriveModule(ClientGame.Current.PlayerShip, SaveHandler.ActiveData.JumpModule);

            //assign session stats.
            GameSessionTracker.Instance._statistics = SaveHandler.ActiveData.SessionStats;

            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.InGameLoad);
        }

        //Blocks Void Drive from disabling itself early
        static bool BlockSpinUpSignalResolution;

        [HarmonyPatch(typeof(VoidJumpSpinningUp), "ResolveSignal")]
        static bool Prefix()
        {
            return !BlockSpinUpSignalResolution;
        }

        [HarmonyPatch(typeof(VoidJumpSpinningUp), "OnExit")]
        static void Postfix()
        {
            BlockSpinUpSignalResolution = false;
        }
    }
}
