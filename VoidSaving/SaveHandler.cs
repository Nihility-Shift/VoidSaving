using CG.Game;
using CG.Ship.Repair;
using CG.Space;
using Gameplay.Quests;
using System;
using System.IO;

namespace VoidSaving
{
    internal class SaveHandler
    {
        public static SaveGameData ActiveData { get; internal set; }

        public static bool StartedAsHost { get; internal set; }

        internal static bool LoadSavedData = false;

        public static string SavesLocation
        {
            get
            {
                return Config.SavesLocation.Value;
            }
            set
            {
                Config.SavesLocation.Value = value;
            }
        }

        internal static SaveGameData GetSessionSaveGameData()
        {
            SaveGameData saveGameData = new SaveGameData();
            GameSession session = GameSessionManager.Instance.activeGameSession;
            AbstractPlayerControlledShip playerShip = ClientGame.Current.PlayerShip;

            //Ship data
            saveGameData.Alloy = GameSessionSuppliesManager.Instance.AlloyAmount;
            saveGameData.Biomass = GameSessionSuppliesManager.Instance.BiomassAmount;
            saveGameData.ShipHealth = ClientGame.Current.PlayerShip.HitPoints;

            saveGameData.ShipLoadout = new ShipLoadout(PlayerShipManager.Instance.ActivePlayerShip).AsJObject();
            saveGameData.Relics = Helpers.RelicGUIDsFromShip(playerShip);
            saveGameData.UnlockedBPs = Helpers.UnlockedBPGUIDsFromShip(playerShip);

            HullDamageController HDC = ClientGame.Current.PlayerShip.GetComponent<HullDamageController>();
            saveGameData.RepairableShipHealth = HDC.State.repairableHp;
            saveGameData.Breaches = Helpers.BreachesAsConditionsArray(HDC.breaches);

            //Quest data
            EndlessQuest activeQuest = session.ActiveQuest as EndlessQuest;

            saveGameData.seed = activeQuest.QuestParameters.Seed;
            saveGameData.JumpCounter = activeQuest.JumpCounter;
            saveGameData.InterdictionCounter = activeQuest.InterdictionCounter;
            saveGameData.random = activeQuest.Context.Random;


            return saveGameData;
        }

        public static bool LoadSave(string SavePath)
        {
            SaveGameData data = new SaveGameData();

            using (FileStream fileStream = File.OpenRead(SavePath))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {

                }
            }

            ActiveData = data;
            return false;
        }

        public static bool WriteSave(string SavePath)
        {
            try
            {
                FileStream fileStream = File.Create(SavePath);
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {

                }
            }
            catch(Exception ex)
            {
                BepinPlugin.Log.LogError(ex);
            }
            return false;
        }
    }
}
