using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Modules.Shield;
using CG.Ship.Repair;
using CG.Space;
using Gameplay.Atmosphere;
using Gameplay.CompositeWeapons;
using Gameplay.Defects;
using Gameplay.Power;
using Gameplay.Quests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VoidManager.Utilities;

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

        internal static bool LoadSavedData { get { return ActiveData != null; } }

        internal static bool IsIronManMode
        {
            get
            {
                return LatestData.IronManMode;
            }
            set
            {
                LatestData.IronManMode = value;
            }
        }

        internal static string LastSaveName
        {
            get
            {
                return LatestData.FileName;
            }
        }

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


        /// <summary>
        /// Deletes file of given name from save location;
        /// </summary>
        /// <param name="SaveName"></param>
        internal static void DeleteSaveFile(string SaveName)
        {
            File.Delete(Path.Combine(SaveLocation, SaveName + SaveExtension));
        }

        internal static Dictionary<string, SaveFilePeekData> GetPeekedSaveFiles()
        {
            string[] Files = Directory.GetFiles(SaveLocation);
            Dictionary<string, SaveFilePeekData> FilesAndDates = new();

            foreach (string file in Files)
            {
                if (file.EndsWith(SaveExtension))
                {
                    string FileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    FilesAndDates.Add(FileNameWithoutExtension, new SaveFilePeekData(FileNameWithoutExtension, File.GetLastWriteTime(file)));
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

                saveGameData.ProgressionDisabled = !VoidManager.Progression.ProgressionHandler.ProgressionEnabled;

                //Ship data
                saveGameData.Alloy = GameSessionSuppliesManager.Instance.AlloyAmount;
                saveGameData.Biomass = GameSessionSuppliesManager.Instance.BiomassAmount;
                saveGameData.ShipHealth = playerShip.HitPoints;

                saveGameData.ShipLoadoutGUID = GameSessionManager.Instance.PreviousSessionShipLoadout;
                saveGameData.ShipLoadout = new ShipLoadout(playerShip.GetComponent<PlayerShip>()).AsJObject();
                saveGameData.Relics = Helpers.RelicGUIDsFromShip(playerShip);
                saveGameData.UnlockedBPs = Helpers.UnlockedBPGUIDsFromShip(playerShip);
                saveGameData.FabricatorTier = playerShip.GetComponentInChildren<FabricatorModule>().CurrentTier;
                HullDamageController HDC = playerShip.GetComponentInChildren<HullDamageController>();
                saveGameData.RepairableShipHealth = HDC.State.repairableHp;
                saveGameData.Breaches = Helpers.GetBreachStates(HDC);
                saveGameData.Defects = Helpers.GetDefectStates(playerShip.GetComponent<PlayerShipDefectDamageController>());

                List<bool> ShipSystemPoweredValues = new();
                foreach (CellModule module in playerShip.CoreSystems)
                    ShipSystemPoweredValues.Add(module.IsPowered);
                saveGameData.ShipSystemPowerStates = ShipSystemPoweredValues.ToArray();

                BuildSocketController bsc = playerShip.GetComponent<BuildSocketController>();
                List<bool> ModulePoweredValues = new();
                List<WeaponBullets> weaponBullets = new();
                List<float> kPDBullets = new();
                List<bool> shieldDirections = new();
                List<byte> LifeSupportSwitches = new();
                List<byte> AutoMechanicSwitches = new();
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
                    else if (socket.InstalledModule is AutoMechanicModule autoMechanicModule)
                    {
                        AutoMechanicSwitches.Add(autoMechanicModule.TriSwitch.Value);
                    }
                    else if (socket.InstalledModule is LifeSupportModule lifeSupportModule)
                    {
                        LifeSupportSwitches.Add(lifeSupportModule.TemperatureSwitch.Value);
                    }
                }
                saveGameData.ModulePowerStates = ModulePoweredValues.ToArray();
                saveGameData.ShieldDirections = shieldDirections.ToArray();
                saveGameData.KPDBullets = kPDBullets.ToArray();
                saveGameData.WeaponBullets = weaponBullets.ToArray();
                saveGameData.AutoMechanicSwitches = AutoMechanicSwitches.ToArray();
                saveGameData.LifeSupportModeSwitches = LifeSupportSwitches.ToArray();

                saveGameData.BoosterStates = Helpers.GetBoosterStates(playerShip);
                saveGameData.ShieldHealths = Helpers.GetShipShieldHealths(playerShip);
                saveGameData.Enhancements = Helpers.GetEnhancements(playerShip);
                saveGameData.JumpModule = new VoidDriveModuleData(playerShip.GetComponentInChildren<VoidDriveModule>());
                Tuple<AtmosphereValues[], AtmosphereValues[]> AtmosDatas = Helpers.GetAtmosphereValues(playerShip);
                saveGameData.AtmosphereValues = AtmosDatas.Item1;
                saveGameData.AtmosphereBufferValues = AtmosDatas.Item2;
                saveGameData.DoorStates = Helpers.GetDoorStates(playerShip);
                saveGameData.AirlockSafeties = Helpers.GetAirlockSafeties(playerShip);

                ProtectedPowerSystem powerSystem = (ProtectedPowerSystem)playerShip.ShipsPowerSystem;
                saveGameData.ShipPowered = playerShip.ShipsPowerSystem.IsPowered();
                saveGameData.BreakerData = Helpers.GetBreakerData(powerSystem);

                //Quest data
                EndlessQuest activeQuest = session.ActiveQuest as EndlessQuest;

                saveGameData.Seed = activeQuest.QuestParameters.Seed;
                saveGameData.SessionStats = GameSessionTracker.Statistics;

                saveGameData.CompletedSectors = Helpers.GetCompletedSectorDatas(activeQuest);


                //Peek Data
                saveGameData.PeekInfo = $"{playerShip.DisplayName},{saveGameData.JumpCounter + 1},{DateTime.Now.Subtract(saveGameData.SessionStats.QuestStartTime).TotalHours},{saveGameData.ProgressionDisabled}";
            }
            catch (Exception e)
            {
                BepinPlugin.Log.LogError("Failed to save data\n" + e);
                Messaging.Notification("<color=red>Failed to save data.</color>");
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
            InGameLoad = 8,
        }

        static LoadingStage CompletedStages;

        public static void CompleteLoadingStage(LoadingStage stage)
        {
            CompletedStages |= stage;
            BepinPlugin.Log.LogInfo("Completed Loading Stage: " + stage);
            
            if (CompletedStages == (LoadingStage.VoidJumpStart | LoadingStage.AbstractPlayerShipStart | LoadingStage.QuestData | LoadingStage.InGameLoad))
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
            ActiveData = null;
            CompletedStages = LoadingStage.None;
        }

        /// <summary>
        /// Loads file with name from save directory
        /// </summary>
        /// <param name="SaveName">File name</param>
        public static bool LoadSave(string SaveName)
        {
            string FullSavePath = Path.Combine(SaveLocation, SaveName + SaveExtension);

            BepinPlugin.Log.LogInfo("Attempting to load save: " + FullSavePath);

            Directory.CreateDirectory(Path.GetDirectoryName(FullSavePath));

            SaveGameData data = new SaveGameData();
            data.FileName = SaveName;

            try
            {
                using (FileStream fileStream = File.OpenRead(FullSavePath))
                {
                    BepinPlugin.Log.LogInfo($"Starting read save: {fileStream.Length} Bytes");
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        data.SaveDataVersion = reader.ReadUInt32();
                        data.PeekInfo = reader.ReadString();
                        data.IronManMode = reader.ReadBoolean();
                        data.ProgressionDisabled = reader.ReadBoolean();

                        data.Alloy = reader.ReadInt32();
                        data.Biomass = reader.ReadInt32();
                        data.ShipHealth = reader.ReadSingle();
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Wrote {fileStream.Length} Bytes");

                        data.ShipLoadoutGUID = reader.ReadGUIDUnion();
                        data.ShipLoadout = reader.ReadJObject();
                        data.AmmoResourceValues = reader.ReadSingleList();
                        data.PowerResourceValues = reader.ReadSingleList();
                        data.Relics = reader.ReadGUIDUnionArray();
                        data.UnlockedBPs = reader.ReadGUIDUnionArray();
                        data.FabricatorTier = reader.ReadByte();
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Read {fileStream.Position} Bytes");

                        data.RepairableShipHealth = reader.ReadSingle();
                        data.Breaches = reader.ReadByteArray();
                        data.Defects = reader.ReadSByteArray();

                        data.ShipPowered = reader.ReadBoolean();
                        data.BreakerData = reader.ReadBreakers();

                        data.ShipSystemPowerStates = reader.ReadBooleanArray();
                        data.ModulePowerStates = reader.ReadBooleanArray();
                        data.ShieldDirections = reader.ReadBooleanArray();
                        data.Enhancements = reader.ReadEnhancements();
                        data.WeaponBullets = reader.ReadWeaponBullets();
                        data.KPDBullets = reader.ReadSingleArray();
                        data.LifeSupportModeSwitches = reader.ReadByteArray();
                        data.AutoMechanicSwitches = reader.ReadByteArray();
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Read {fileStream.Position} Bytes");

                        data.BoosterStates = reader.ReadBoosterStatuses();
                        data.ShieldHealths = reader.ReadSingleArray();
                        data.JumpModule = reader.ReadVoidDriveData();
                        data.AtmosphereValues = reader.ReadAtmosphereValues();
                        data.AtmosphereBufferValues = reader.ReadAtmosphereValues();
                        data.DoorStates = reader.ReadBooleanArray();
                        data.AirlockSafeties = reader.ReadBooleanArray();
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Read {fileStream.Position} Bytes");

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
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Read {fileStream.Position} Bytes");

                        data.CompletedSectors = reader.ReadSectors();
                        data.SessionStats = reader.ReadSessionStats();

                        BepinPlugin.Log.LogInfo($"Finalized read at {fileStream.Position} Bytes");
                    }
                }
            }
            catch (Exception ex)
            {
                BepinPlugin.Log.LogError($"Failed to load save {FullSavePath}\n{ex.Message}");
                return false;
            }

            ActiveData = data;
            Messaging.Echo($"Loading save '{SaveName}' on next game start.", false);
            if(data.ProgressionDisabled)
                Messaging.Echo("Progress will be disabled after starting.");
            return true;
        }

        public static SaveGameData PeekSaveFile(string SaveName)
        {
            string FullSavePath = Path.Combine(SaveLocation, SaveName + SaveExtension);

            BepinPlugin.Log.LogInfo("Attempting to peek save: " + FullSavePath);

            Directory.CreateDirectory(Path.GetDirectoryName(FullSavePath));

            SaveGameData data = new();

            try
            {
                using (FileStream fileStream = File.OpenRead(FullSavePath))
                {
                    BepinPlugin.Log.LogInfo($"Starting peek save: {fileStream.Length} Bytes");
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        data.SaveDataVersion = reader.ReadUInt32();
                        data.PeekInfo = reader.ReadString();
                        data.IronManMode = reader.ReadBoolean();
                    }
                }
            }
            catch (Exception ex)
            {
                BepinPlugin.Log.LogError($"Failed to peek save {FullSavePath}\n{ex.Message}");
            }

            return data;
        }


        public const uint CurrentDataVersion = 0;

        /// <summary>
        /// Writes file to path. Adds extension if missing.
        /// </summary>
        /// <param name="FileName">File name with/without extension</param>
        /// <returns>Success</returns>
        public static bool WriteSave(string FileName)
        {
            string fullSavePath = Path.Combine(SaveLocation, FileName);

            BepinPlugin.Log.LogInfo("Attempting to write save: " + fullSavePath);

            if (!fullSavePath.EndsWith(SaveExtension))
            {
                fullSavePath += SaveExtension;
            }

            string safePath = fullSavePath + ".safe";

            Directory.CreateDirectory(Path.GetDirectoryName(fullSavePath));

            SaveGameData data = GetSessionSaveGameData();
            data.FileName = FileName;
            try
            {
                using (FileStream fileStream = File.Create(safePath))
                {
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        writer.Write(CurrentDataVersion);
                        writer.Write(data.PeekInfo);
                        writer.Write(data.IronManMode);
                        writer.Write(data.ProgressionDisabled);

                        writer.Write(data.Alloy);
                        writer.Write(data.Biomass);
                        writer.Write(data.ShipHealth);
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Wrote {fileStream.Length} Bytes");

                        writer.Write(data.ShipLoadoutGUID);
                        writer.Write(data.ShipLoadout);
                        writer.Write(data.AmmoResourceValues);
                        writer.Write(data.PowerResourceValues);
                        writer.Write(data.Relics);
                        writer.Write(data.UnlockedBPs);
                        writer.Write((byte)data.FabricatorTier);
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Wrote {fileStream.Length} Bytes");

                        writer.Write(data.RepairableShipHealth);
                        writer.WriteByteArray(data.Breaches);
                        writer.WriteSByteArray(data.Defects);

                        writer.Write(data.ShipPowered);
                        writer.Write(data.BreakerData);
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Wrote {fileStream.Length} Bytes");

                        writer.Write(data.ShipSystemPowerStates);
                        writer.Write(data.ModulePowerStates);
                        writer.Write(data.ShieldDirections);
                        writer.Write(data.Enhancements);
                        writer.Write(data.WeaponBullets);
                        writer.Write(data.KPDBullets);
                        writer.WriteByteArray(data.LifeSupportModeSwitches);
                        writer.WriteByteArray(data.AutoMechanicSwitches);
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Wrote {fileStream.Length} Bytes");

                        writer.Write(data.BoosterStates);
                        writer.Write(data.ShieldHealths);
                        writer.Write(data.JumpModule);
                        writer.Write(data.AtmosphereValues);
                        writer.Write(data.AtmosphereBufferValues);
                        writer.Write(data.DoorStates);
                        writer.Write(data.AirlockSafeties);
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Wrote {fileStream.Length} Bytes");

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
                        if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Wrote {fileStream.Length} Bytes");

                        writer.Write(data.CompletedSectors);
                        writer.Write(data.SessionStats);

                        BepinPlugin.Log.LogInfo($"Finalized write at {fileStream.Length} Bytes");
                    }
                }
                LatestData = data;
                File.Delete(fullSavePath);
                File.Move(safePath, fullSavePath);
                return true;
            }
            catch (Exception ex)
            {
                Messaging.Notification("<color=red>Failed to Write Save!</color>");
                BepinPlugin.Log.LogError(ex);
                File.Delete(safePath);
            }

            return false;
        }

        public static bool WriteIronManSave(string FileName)
        {
            string oldSaveName = LastSaveName;
            if (WriteSave(FileName) && FileName != oldSaveName)
            {
                BepinPlugin.Log.LogInfo($"Iron Man Save succesfully wrote {FileName}, deleting old file {oldSaveName}");
                DeleteSaveFile(oldSaveName);
                return true;
            }
            return false;
        }

        internal static string IronManSaveDefaultNameFromID(int SaveID)
        {
            return $"IronManSave_{SaveID.ToString("D2")}";
        }

        public static string GetNextIronManSaveName()
        {
            int LastIronManSave = 1;
            while(File.Exists(Path.Combine(SaveLocation, $"IronManSave_{LastIronManSave.ToString("D2")}{SaveExtension}")))
            {
                LastIronManSave++;
            }
            return IronManSaveDefaultNameFromID(LastIronManSave);
        }
    }
}
