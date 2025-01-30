using CG.GameLoopStateMachine.GameStates;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch(typeof(GSQuitFromMenu), "OnEnter")]
    internal class LeaveGameCancelLoadPatch
    {
        static void Postfix()
        {
            SaveHandler.CancelOrFinalzeLoad();
        }
    }
}
