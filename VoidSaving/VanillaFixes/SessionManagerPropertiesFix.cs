using HarmonyLib;

namespace VoidSaving.VanillaFixes
{
    //Fixes issue with loading where the sector is entered and exited faster than room properties update, leading to the game beleiving the sector was entered and exited twice.
    [HarmonyPatch(typeof(GameSessionSectorManager), "OnRoomPropertiesUpdate")]
    internal class SessionManagerPropertiesFix
    {
        internal static bool BlockLoadingExecution;

        static bool Prefix()
        {
            if (BlockLoadingExecution)
            {
                BlockLoadingExecution = false;
                return false;
            }
            return true;
        }
    }
}
