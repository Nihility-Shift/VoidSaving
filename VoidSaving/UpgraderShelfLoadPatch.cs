using HarmonyLib;

namespace VoidSaving
{
    //Upgraders being placed in shelves on ship load break here. Stop original code from running if attempting to run (no calls needed from vanilla at time of writing)
    [HarmonyPatch(typeof(ModuleUpgraderEffects), "StartBeingCarried")]
    internal class UpgraderShelfLoadPatch
    {
        static bool Prefix()
        {
            if (SaveHandler.LoadSavedData)
            {
                return false;
            }
            return true;
        }
    }
}
