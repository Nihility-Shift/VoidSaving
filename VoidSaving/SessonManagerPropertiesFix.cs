using HarmonyLib;
using Photon.Pun;

namespace VoidSaving
{
    [HarmonyPatch(typeof(GameSessionSectorManager), "OnRoomPropertiesUpdate")]
    internal class SessonManagerPropertiesFix
    {
        static bool Prefix()
        {
            //If master client, value should have already been processed.
            return !PhotonNetwork.IsMasterClient;
        }
    }
}
