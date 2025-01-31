using CG.GameLoopStateMachine;
using CG.GameLoopStateMachine.GameStates;
using HarmonyLib;
using VoidManager.Utilities;

namespace VoidSaving
{
    [HarmonyPatch(typeof(GSIngame), "OnEnter")]
    internal class IronManNotifiyPatch
    {
        static void Postfix(IState previous)
        {
            if (previous is GSSpawn && GameSessionManager.InHub && SaveHandler.StartedAsHost)
            {
                //Reset latest data so next saves don't carry data over.
                SaveHandler.LatestData = null;
                if (SaveHandler.IsIronManMode)
                {
                    Messaging.Notification("Iron Man for the next session is ON");
                }
            }
        }
    }
}
