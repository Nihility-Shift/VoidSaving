using Gameplay.Quests;
using HarmonyLib;
using UI.AstralMap;

namespace VoidSaving
{
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
