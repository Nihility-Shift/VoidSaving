using CG.GameLoopStateMachine.GameStates;
using HarmonyLib;

namespace VoidSaving.Patches
{
    [HarmonyPatch(typeof(GSEndSession), "OnEnter")]
    internal class IronManDeleteOnWinPatch
    {
        static void Postfix()
        {
            if (SaveHandler.StartedAsHost && SaveHandler.IsIronManMode)
            {
                SaveHandler.DeleteSaveFile(SaveHandler.LastSaveName);
            }
        }
    }
}
