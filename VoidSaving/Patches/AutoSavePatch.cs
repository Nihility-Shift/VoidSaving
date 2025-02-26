using CG.Game.SpaceObjects.Controllers;
using HarmonyLib;

namespace VoidSaving.Patches
{
    [HarmonyPatch(typeof(VoidJumpSystem), "EnterVoid")]
    internal class AutoSavePatch
    {
        internal static bool FirstJump;
        static void Postfix()
        {
            if (!Config.AutoSavingEnabled.Value || SaveHandler.LoadSavedData || !SaveHandler.StartedAsHost) return;
            if (FirstJump) { FirstJump = false; return; }


            if (SaveHandler.IsIronManMode)
            {
                if (SaveHandler.LastSaveName == null)
                {
                    SaveHandler.LatestData.FileName = SaveHandler.GetNextIronManSaveName();
                }
                SaveHandler.WriteSave(SaveHandler.LastSaveName);
                return;
            }


            Config.LastAutoSave.Value++;
            if (Config.LastAutoSave.Value > Config.AutoSaveLimit.Value)
            {
                Config.LastAutoSave.Value = 1;
            }
            SaveHandler.WriteSave($"AutoSave_{Config.LastAutoSave.Value}");
        }
    }
}
