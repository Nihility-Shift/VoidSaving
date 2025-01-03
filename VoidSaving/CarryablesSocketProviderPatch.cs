using CG.Ship.Modules;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch(typeof(CarryablesSocketProvider), "Awake")]
    internal class CarryablesSocketProviderPatch
    {
        static void Prefix(CarryablesSocketProvider __instance)
        {
            //Never called befure, but required for socket provider to be aware of sockets. This is called to help the existing ship save system detect installed mods/batteries.
            __instance.FindSockets();
        }
    }
}
