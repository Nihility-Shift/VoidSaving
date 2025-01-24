using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Modules.Shield;
using CG.Ship.Repair;
using CG.Space;
using Gameplay.CompositeWeapons;
using Gameplay.Defects;
using Gameplay.Power;
using Gameplay.Quests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VoidSaving
{
    internal class SaveHandler
    {
        public const string SaveFolderName = "Saves";

        public const string SaveExtension = ".voidsave";

        public static string SaveLocation
        {
            get
            {
                return $"{Directory.GetCurrentDirectory()}\\{SaveFolderName}";
            }
        }

        public static SaveGameData ActiveData { get; internal set; }


        //Captured Data from current run (must grab prior to mid-void to avoid dirty data)

        private static SaveGameData m_LatestData;

        internal static SaveGameData LatestData { 
            get
            {
                if (m_LatestData == null)
                {
                    m_LatestData = new SaveGameData();
                }
                return m_LatestData;
            }
            set
            {
                m_LatestData = value;
            }
        }

        public static bool StartedAsHost { get; internal set; }

        internal static bool LoadSavedData = false;


        /*public static string SavesLocation
        {
            get
            {
                return Config.SavesLocation.Value;
            }
            set
            {
                Config.SavesLocation.Value = value;
            }
        }*/

        internal static void DeleteSaveFile(string SaveName)
        {
            File.Delete(Path.Combine(SaveLocation, SaveName));
        }

        internal static Dictionary<string, DateTime> GetSaveFileNames()
        {
            string[] Files = Directory.GetFiles(SaveLocation);
            Dictionary<string, DateTime> FilesAndDates = new();

            foreach (string file in Files)
            {
                if (file.EndsWith(SaveExtension))
                {
                    FilesAndDates.Add(Path.GetFileNameWithoutExtension(file), Directory.GetLastWriteTime(file));
                }
            }

            //sort by date
            FilesAndDates.OrderBy(x => x.Value);

            return FilesAndDates;
        }



        internal static SaveGameData GetSessionSaveGameData()
        {
            SaveGameData saveGameData = LatestData;
            try
            {
            GameSession session = GameSessionManager.Instance.activeGameSession;
            AbstractPlayerControlledShip playerShip = ClientGame.Current.PlayerShip;

            //Ship data
            saveGameData.Alloy = GameSessionSuppliesManager.Instance.AlloyAmount;
            saveGameData.Biomass = GameSessionSuppliesManager.Instance.BiomassAmount;
            saveGameData.ShipHealth = playerShip.HitPoints;

            saveGameData.ShipLoadoutGUID = GameSessionManager.Instance.PreviousSessionShipLoadout;
            saveGameData.ShipLoadout = new ShipLoadout(playerShip.GetComponent<PlayerShip>()).AsJObject();
            saveGameData.Relics = Helpers.RelicGUIDsFromShip(playerShip);
            saveGameData.UnlockedBPs = Helpers.UnlockedBPGUIDsFromShip(playerShip);
            saveGameData.FabricatorTier = playerShip.GetComponent<FabricatorModule>().CurrentTier;

            PlayerShipDefectDamageController PSDDC = playerShip.GetComponent<PlayerShipDefectDamageController>();
            saveGameData.RepairableShipHealth = PSDDC._hullDamageController.State.repairableHp;
            saveGameData.Breaches = Helpers.GetBreachStates(PSDDC._hullDamageController);
            saveGameData.Defects = Helpers.GetDefectStates(PSDDC);

            List<bool> ShipSystemPoweredValues = new();
            foreach (CellModule module in playerShip.CoreSystems)
            {
                ShipSystemPoweredValues.Add(module.IsPowered);
            }
            saveGameData.ShipSystemPowerStates = ShipSystemPoweredValues.ToArray();

            BuildSocketController bsc = playerShip.GetComponent<BuildSocketController>();
            List<bool> ModulePoweredValues = new();
            List<WeaponBullets> weaponBullets = new();
            List<float> kPDBullets = new();
            List<bool> shieldDirections = new();
            foreach (BuildSocket socket in bsc.Sockets)
            {
                if (socket.InstalledModule == null) continue;


                ModulePoweredValues.Add(socket.InstalledModule.IsPowered);

                if (socket.InstalledModule is CompositeWeaponModule weaponModule && weaponModule.InsideElementsCollection.Magazine is BulletMagazine magazine)
                {
                    weaponBullets.Add(new WeaponBullets(magazine.AmmoLoaded, magazine.ReservoirAmmoCount));
                }
                else if (socket.InstalledModule is KineticPointDefenseModule KPDModule)
                {
                    kPDBullets.Add(KPDModule.AmmoCount);
                }
                else if (socket.InstalledModule is ShieldModule shieldModule)
                {
                    shieldDirections.Add(shieldModule.IsClockwise.Value);
                    shieldDirections.Add(shieldModule.IsForward.Value);
                    shieldDirections.Add(shieldModule.IsCounterClockwise.Value);
                }
            }
            saveGameData.ModulePowerStates = ModulePoweredValues.ToArray();
            saveGameData.ShieldDirections = shieldDirections.ToArray();
            saveGameData.KPDBullets = kPDBullets.ToArray();
            saveGameData.WeaponBullets = weaponBullets.ToArray();

            saveGameData.BoosterStates = Helpers.GetBoosterStates(playerShip);
            saveGameData.ShieldHealths = Helpers.GetShipShieldHealths(playerShip);
            saveGameData.Enhancements = Helpers.GetEnhancements(playerShip);
            saveGameData.JumpModule = new VoidDriveModuleData(playerShip.GetComponentInChildren<VoidDriveModule>());
            saveGameData.AtmosphereValues = Helpers.GetAtmosphereValues(playerShip);
            saveGameData.DoorStates = Helpers.GetDoorStates(playerShip);
            saveGameData.AirlockSafeties = Helpers.GetAirlockSafeties(playerShip);

            ProtectedPowerSystem powerSystem = (ProtectedPowerSystem)playerShip.ShipsPowerSystem;
            saveGameData.ShipPowered = playerShip.ShipsPowerSystem.IsPowered();
            saveGameData.BreakerData = Helpers.GetBreakerData(powerSystem);

            //Quest data
            EndlessQuest activeQuest = session.ActiveQuest as EndlessQuest;

            saveGameData.Seed = activeQuest.QuestParameters.Seed;
            saveGameData.JumpCounter = activeQuest.JumpCounter;
            saveGameData.InterdictionCounter = activeQuest.InterdictionCounter;
            saveGameData.SessionStats = GameSessionTracker.Statistics;

            saveGameData.CompletedSectors = Helpers.GetCompletedSectorDatas(activeQuest.context.CompletedSectors);
            }
            catch (Exception e)
            {
                BepinPlugin.Log.LogError("Failed to save data\n" + e);
                Messaging.Notification("Failed to save data.");
            }
            return saveGameData;
        }

        [Flags]
        public enum LoadingStage
        {
            None = 0,
            VoidJumpStart = 1,
            AbstractPlayerShipStart = 2,
            QuestData = 4,
        }

        static LoadingStage CompletedStages;

        public static void CompleteLoadingStage(LoadingStage stage)
        {
            CompletedStages |= stage;
            BepinPlugin.Log.LogInfo("Completed Loading Stage: " + stage);
            
            if (CompletedStages == (LoadingStage.VoidJumpStart | LoadingStage.AbstractPlayerShipStart | LoadingStage.QuestData))
            {
                BepinPlugin.Log.LogInfo("Finished all loading stages");
                LatestData = ActiveData;
                CancelOrFinalzeLoad();
            }
        }

        /// <summary>
        /// Cancels or clears loading of save data
        /// </summary>
        public static void CancelOrFinalzeLoad()
        {
            LoadSavedData = false;
            ActiveData = null;
            CompletedStages = LoadingStage.None;
        }

        /// <summary>
        /// Loads file with name from save directory
        /// </summary>
        /// <param name="SaveName">File name and extension</param>
        public static bool LoadSave(string SaveName)
        {
            SaveName = Path.Combine(SaveLocation, SaveName);

            BepinPlugin.Log.LogInfo("Attempting to load save: " + SaveName);

            Directory.CreateDirectory(Path.GetDirectoryName(SaveName));

            SaveGameData data = new SaveGameData();
            data.FileName = SaveName.Replace(SaveExtension, string.Empty);

            try
            {
                using (FileStream fileStream = File.OpenRead(SaveName))
                {
                    BepinPlugin.Log.LogInfo($"Starting read save: {fileStream.Length} Bytes");
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        data.SaveDataVersion = reader.ReadUInt32();
                        data.Alloy = reader.ReadInt32();
                        data.Biomass = reader.ReadInt32();
                        data.ShipHealth = reader.ReadSingle();

                        data.ShipLoadoutGUID = reader.ReadGUIDUnion();
                        data.ShipLoadout = reader.ReadJObject();
                        data.AmmoResourceValues = reader.ReadSingleList();
                        data.PowerResourceValues = reader.ReadSingleList();
                        data.Relics = reader.ReadGUIDUnionArray();
                        data.UnlockedBPs = reader.ReadGUIDUnionArray();
                        data.FabricatorTier = reader.ReadByte();

                        data.RepairableShipHealth = reader.ReadSingle();
                        data.Breaches = reader.ReadByteArray();
                        data.Defects = reader.ReadByteArray();

                        data.ShipPowered = reader.ReadBoolean();
                        data.BreakerData = reader.ReadBreakers();

                        data.ShipSystemPowerStates = reader.ReadBooleanArray();
                        data.ModulePowerStates = reader.ReadBooleanArray();
                        data.ShieldDirections = reader.ReadBooleanArray();
                        data.Enhancements = reader.ReadEnhancements();
                        data.WeaponBullets = reader.ReadWeaponBullets();
                        data.KPDBullets = reader.ReadSingleArray();

                        data.BoosterStates = reader.ReadBoosterStatuses();
                        data.ShieldHealths = reader.ReadSingleArray();
                        data.JumpModule = reader.ReadVoidDriveData();
                        data.AtmosphereValues = reader.ReadAtmosphereValues();
                        data.DoorStates = reader.ReadBooleanArray();
                        data.AirlockSafeties = reader.ReadBooleanArray();

                        data.Seed = reader.ReadInt32();
                        data.ParametersSeed = reader.ReadInt32();
                        data.JumpCounter = reader.ReadInt32();
                        data.InterdictionCounter = reader.ReadInt32();
                        data.CurrentInterdictionChance = reader.ReadSingle();
                        data.Random = reader.ReadRandom();
                        data.NextSectorID = reader.ReadInt32();
                        data.ActiveSolarSystemID = reader.ReadInt32();
                        data.NextSolarSystemID = reader.ReadInt32();
                        data.NextSectionIndex = reader.ReadInt32();
                        data.EnemyLevelRangeMin = reader.ReadInt32();
                        data.EnemyLevelRangeMax = reader.ReadInt32();
                        data.SectorsUsedInSolarSystem = reader.ReadInt32();
                        data.SectorsToUseInSolarSystem = reader.ReadInt32();

                        data.CompletedSectors = reader.ReadSectors();
                        data.SessionStats = reader.ReadSessionStats();

                        BepinPlugin.Log.LogInfo($"Read {fileStream.Position} Bytes");
                    }
                }
            }
            catch (Exception ex)
            {
                BepinPlugin.Log.LogError($"Failed to load save {SaveName}\n{ex.Message}");
                return false;
            }

            LoadSavedData = true;
            ActiveData = data;
            return true;
        }

        /// <summary>
        /// Writes file to path. Adds extension if missing.
        /// </summary>
        /// <param name="SavePath">Full save path with/without extension</param>
        /// <returns>Success</returns>
        public static bool WriteSave(string SavePath)
        {
            BepinPlugin.Log.LogInfo("Attempting to write save: " + SavePath);

            if (!SavePath.EndsWith(SaveExtension))
            {
                SavePath += SaveExtension;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));

            SaveGameData data = GetSessionSaveGameData();
            try
            {
                using (FileStream fileStream = File.Create(SavePath))
                {
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        writer.Write(data.SaveDataVersion);
                        writer.Write(data.Alloy);
                        writer.Write(data.Biomass);
                        writer.Write(data.ShipHealth);

                        writer.Write(data.ShipLoadoutGUID);
                        writer.Write(data.ShipLoadout);
                        writer.Write(data.AmmoResourceValues);
                        writer.Write(data.PowerResourceValues);
                        writer.Write(data.Relics);
                        writer.Write(data.UnlockedBPs);
                        writer.Write((byte)data.FabricatorTier);

                        writer.Write(data.RepairableShipHealth);
                        writer.WriteByteArray(data.Breaches);
                        writer.WriteByteArray(data.Defects);

                        writer.Write(data.ShipPowered);
                        writer.Write(data.BreakerData);

                        writer.Write(data.ShipSystemPowerStates);
                        writer.Write(data.ModulePowerStates);
                        writer.Write(data.ShieldDirections);
                        writer.Write(data.Enhancements);
                        writer.Write(data.WeaponBullets);
                        writer.Write(data.KPDBullets);

                        writer.Write(data.BoosterStates);
                        writer.Write(data.ShieldHealths);
                        writer.Write(data.JumpModule);
                        writer.Write(data.AtmosphereValues);
                        writer.Write(data.DoorStates);
                        writer.Write(data.AirlockSafeties);

                        writer.Write(data.Seed);
                        writer.Write(data.ParametersSeed);
                        writer.Write(data.JumpCounter);
                        writer.Write(data.InterdictionCounter);
                        writer.Write(data.CurrentInterdictionChance);
                        writer.Write(data.Random);
                        writer.Write(data.NextSectorID);
                        writer.Write(data.ActiveSolarSystemID);
                        writer.Write(data.NextSolarSystemID);
                        writer.Write(data.NextSectionIndex);
                        writer.Write(data.EnemyLevelRangeMin);
                        writer.Write(data.EnemyLevelRangeMax);
                        writer.Write(data.SectorsUsedInSolarSystem);
                        writer.Write(data.SectorsToUseInSolarSystem);

                        writer.Write(data.CompletedSectors);
                        writer.Write(data.SessionStats);

                        BepinPlugin.Log.LogInfo($"Wrote {fileStream.Length} Bytes");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                BepinPlugin.Log.LogError(ex);
            }
            return false;
        }
    }
}
