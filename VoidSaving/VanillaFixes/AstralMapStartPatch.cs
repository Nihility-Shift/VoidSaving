using Gameplay.Quests;
using HarmonyLib;
using UI.AstralMap;

namespace VoidSaving.VanillaFixes
{
    //Map thinks it's not the starting sector if completed sectors has data, so replace with decent check code.
    [HarmonyPatch(typeof(AstralMapController), "CheckForGameStart")]
    internal class AstralMapStartPatch
    {
        static bool Prefix(AstralMapController __instance, EndlessQuest eq)
        {
            __instance._gameStart = __instance._lastSector == null || eq.StartSector == __instance._lastSector.Sector;
            return false;
        }
    }
}
