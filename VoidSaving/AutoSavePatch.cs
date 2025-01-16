using CG.Game.SpaceObjects.Controllers;
using Client.Utils;
using Gameplay.Quests;
using HarmonyLib;
using System.IO;

namespace VoidSaving
{
    [HarmonyPatch(typeof(VoidJumpSystem), "EnterVoid")]
    internal class AutoSavePatch
    {
        static void Prefix()
        {
            //Capture current random for saving prior to generation of next section.
            if (!SaveHandler.LoadSavedData) SaveHandler.LatestRandom = ((EndlessQuest)GameSessionManager.Instance.activeGameSession.ActiveQuest).Context.Random.DeepCopy();
        }
        static void Postfix()
        {
            if (!Config.AutoSavingEnabled.Value || SaveHandler.LoadSavedData) return;

            Config.LastAutoSave.Value++;
            if (Config.LastAutoSave.Value > Config.AutoSaveLimit.Value)
            {
                Config.LastAutoSave.Value = 1;
            }
            SaveHandler.WriteSave(Path.Combine(SaveHandler.SaveLocation, $"AutoSave_{Config.LastAutoSave.Value}"));
        }
    }
}
