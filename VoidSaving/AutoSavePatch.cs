using CG.Game.SpaceObjects.Controllers;
using HarmonyLib;
using System.IO;

namespace VoidSaving
{
    [HarmonyPatch(typeof(VoidJumpSystem), "EnterVoid")]
    internal class AutoSavePatch
    {
        static void Postfix()
        {
            if (!Config.AutoSavingEnabled.Value || SaveHandler.LoadSavedData || !SaveHandler.StartedAsHost) return;

            Config.LastAutoSave.Value++;
            if (Config.LastAutoSave.Value > Config.AutoSaveLimit.Value)
            {
                Config.LastAutoSave.Value = 1;
            }
            SaveHandler.WriteSave($"AutoSave_{Config.LastAutoSave.Value}");
        }
    }
}
