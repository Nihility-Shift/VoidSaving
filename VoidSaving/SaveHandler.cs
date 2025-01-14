﻿using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using CG.Ship.Hull;
using CG.Ship.Repair;
using CG.Space;
using Gameplay.Power;
using Gameplay.Quests;
using System;
using System.Collections;
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
            SaveGameData saveGameData = new SaveGameData();
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

            HullDamageController HDC = playerShip.GetComponentInChildren<HullDamageController>();
            saveGameData.RepairableShipHealth = HDC.State.repairableHp;
            saveGameData.Breaches = Helpers.BreachesAsConditionsArray(HDC.breaches);

            List<bool> PoweredValues = new();
            BuildSocketController bsc = playerShip.GetComponent<BuildSocketController>();
            foreach (BuildSocket socket in bsc.Sockets)
            {
                if (socket.InstalledModule != null)
                {
                    PoweredValues.Add(socket.InstalledModule.IsPowered);
                }
            }
            saveGameData.ModulePowerStates = PoweredValues.ToArray();

            ProtectedPowerSystem powerSystem = (ProtectedPowerSystem)playerShip.ShipsPowerSystem;
            saveGameData.ShipPowered = powerSystem.IsPowered();

            //Quest data
            EndlessQuest activeQuest = session.ActiveQuest as EndlessQuest;

            saveGameData.seed = activeQuest.QuestParameters.Seed;
            saveGameData.JumpCounter = activeQuest.JumpCounter;
            saveGameData.InterdictionCounter = activeQuest.InterdictionCounter;
            saveGameData.random = activeQuest.Context.Random;


            return saveGameData;
        }

        /// <summary>
        /// Cancels load of save data
        /// </summary>
        public static void CancelLoad()
        {
            LoadSavedData = false;
            ActiveData = null;
        }

        /// <summary>
        /// Loads file with name from save directory
        /// </summary>
        /// <param name="SaveName">File name and extension</param>
        public static void LoadSave(string SaveName)
        {
            SaveName = Path.Combine(SaveLocation, SaveName);

            BepinPlugin.Log.LogInfo("Attempting to load save: " + SaveName);

            Directory.CreateDirectory(Path.GetDirectoryName(SaveName));

            SaveGameData data = new SaveGameData();

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
                    data.Relics = reader.ReadGUIDUnionArray();
                    data.UnlockedBPs = reader.ReadGUIDUnionArray();

                    data.RepairableShipHealth = reader.ReadSingle();
                    data.Breaches = Array.ConvertAll(reader.ReadInt32Array(), value => (BreachCondition)value);

                    data.ShipPowered = reader.ReadBoolean();

                    data.ModulePowerStates = reader.ReadBooleanArray();

                    data.seed = reader.ReadInt32();
                    data.JumpCounter = reader.ReadInt32();
                    data.InterdictionCounter = reader.ReadInt32();
                    data.random = reader.ReadRandom();
                }
            }

            LoadSavedData = true;
            ActiveData = data;
        }

        /// <summary>
        /// Writes file to path. Adds extension if missing.
        /// </summary>
        /// <param name="SavePath">Full save path with/without extension</param>
        /// <returns>Success</returns>
        public static bool WriteSave(string SavePath)
        {
            BepinPlugin.Log.LogInfo("Attempting to write save: " + SavePath);

            if(!SavePath.EndsWith(SaveExtension))
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
                        writer.Write(data.Relics);
                        writer.Write(data.UnlockedBPs);

                        writer.Write(data.RepairableShipHealth);
                        writer.Write(Array.ConvertAll(data.Breaches, value => (int)value));

                        writer.Write(data.ShipPowered);

                        writer.Write(data.ModulePowerStates);

                        writer.Write(data.seed);
                        writer.Write(data.JumpCounter);
                        writer.Write(data.InterdictionCounter);
                        writer.Write(data.random);
                    }
                    BepinPlugin.Log.LogInfo($"Finished writing save: {fileStream.Length} Bytes");
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
