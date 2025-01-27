using CG.Cloud;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch(typeof(CloudPlayerProfileDataSync), "ClaimPendingXp")]
    internal class XPProfileLoadPatch
    {
        static void Postfix(CloudPlayerProfileDataSync __instance)
        {
            __instance.UploadProfile();
        }
    }
}
