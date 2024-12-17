using CG.Client.UI;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch(typeof(FadeController), "Start")]
    internal class Patch
    {
        static void Postfix()
        {
            BepinPlugin.Log.LogInfo("Example Patch Executed");
        }
    }
}
