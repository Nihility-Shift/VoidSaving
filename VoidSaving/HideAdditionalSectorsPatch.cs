using Gameplay.Quests;
using HarmonyLib;
using System.Collections.Generic;

namespace VoidSaving
{
    [HarmonyPatch(typeof(GameSessionSection), "AllAvailableSectors", MethodType.Getter)]
    internal class HideAdditionalSectorsPatch
    {
        //Prefix replacement somehow created an infinite load loop.
        static void Postfix(GameSessionSection __instance, ref List<GameSessionSector> __result)
        {
            List<GameSessionSector> list = new List<GameSessionSector>(__instance.ObjectiveSectors);
            if (__instance.InterdictionSector != null) list.Add(__instance.InterdictionSector);
            __result = list;
        }
    }
}
