using CG.Game.SpaceObjects.Controllers;
using HarmonyLib;
using System.IO;

namespace VoidSaving
{
    [HarmonyPatch(typeof(VoidJumpSystem), "EnterVoid")]
    internal class AutoSavePatch
    {
        static int AutoSaveCount = 0;
        static void Postfix()
        {
            if (!Config.AutoSavingEnabled.Value) return;

            AutoSaveCount++;
            if (AutoSaveCount > Config.AutoSaveLimit.Value)
            {
                AutoSaveCount = 1;
            }
            SaveHandler.WriteSave(Path.Combine(SaveHandler.SaveLocation, $"AutoSave_{AutoSaveCount}.voidsave"));
        }
    }
}
