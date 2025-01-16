using Gameplay.Quests;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch(typeof(Quest), "NextJumpInterdictionCheck")]
    internal class CapturePreJumpPatch
    {
        static void Prefix(Quest __instance)
        {
            SaveHandler.LatestCurrentInterdictionChance = __instance.CurrentInterdictionChance;
        }
    }
}
