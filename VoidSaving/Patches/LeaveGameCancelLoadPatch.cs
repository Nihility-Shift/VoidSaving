using CG.GameLoopStateMachine.GameStates;
using HarmonyLib;

namespace VoidSaving.Patches
{
    [HarmonyPatch(typeof(GSQuitFromMenu), "OnEnter")]
    internal class LeaveGameCancelLoadPatch
    {
        static void Postfix()
        {
            SaveHandler.LatestData = null;
            SaveHandler.CancelOrFinalzeLoad();
        }
    }
}
