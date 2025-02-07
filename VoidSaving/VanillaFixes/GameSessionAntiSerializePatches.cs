using Gameplay.Quests;
using HarmonyLib;

namespace VoidSaving.VanillaFixes
{
    [HarmonyPatch(typeof(GameSession), "AsJObject")]
    internal class GameSessionAntiSerializePatches
    {
        internal static bool Serializing;

        static void Prefix()
        {
            Serializing = true;
        }

        static void Postfix()
        {
            Serializing = false;
        }

        [HarmonyPatch(typeof(EndlessQuest), "InterdictionSector", MethodType.Getter), HarmonyPrefix]
        static bool InterdictionStopSerialize() //GameSessionSector __result
        {
            return !Serializing;
        }
    }
}
