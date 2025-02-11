using HarmonyLib;
using Photon.Pun;

namespace VoidSaving.VanillaFixes
{
    [HarmonyPatch(typeof(GameSessionSectorManager), "OnRoomPropertiesUpdate")]
    internal class SessionManagerPropertiesFix
    {
        static bool Prefix()
        {
            //If master client, value should have already been processed.
            return !PhotonNetwork.IsMasterClient;
        }
    }
}
