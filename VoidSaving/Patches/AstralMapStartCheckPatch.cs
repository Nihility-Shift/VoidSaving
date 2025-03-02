using HarmonyLib;
using UI.AstralMap;

namespace VoidSaving.Patches
{
    //While loading a save created immediately after the starting sector, the map falsly detects the game as not being immediately after the start section.
    [HarmonyPatch(typeof(AstralMapController), "CheckForGameStart")]
    class AstralMapStartCheckPatch
    {
        static bool Prefix(AstralMapController __instance)
        {
            if (GameSessionManager.ActiveSession.ActiveSectorId == 0)
            {
                __instance._gameStart = true;
                return false;
            }
            return true;
        }
    }
}
