using HarmonyLib;

namespace VoidSaving.Patches
{
    [HarmonyPatch]
    internal class LoadInInterdictionTimerPatches
    {
        [HarmonyPatch(typeof(VoidJumpTravellingStable), "OnEnter"), HarmonyPostfix]
        static void VoidJumpStableInterdictionTimerPatch(VoidJumpTravellingStable __instance)
        {
            if (SaveHandler.LoadSavedData)
            {
                __instance.DurationUntilUnstable += Config.ExtraMSUntilInterdiction.Value;
            }
        }

        [HarmonyPatch(typeof(VoidJumpTravellingUnstable), "OnEnter"), HarmonyPostfix]
        static void VoidJumpUnstableInterdictionTimerPatch(VoidJumpTravellingUnstable __instance)
        {
            if (SaveHandler.LoadSavedData)
            {
                __instance.DurationUntilInterdiction += Config.ExtraMSUntilInterdiction.Value;
            }
        }
    }
}
