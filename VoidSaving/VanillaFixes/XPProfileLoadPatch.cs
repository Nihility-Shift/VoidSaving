using CG.Cloud;
using HarmonyLib;

namespace VoidSaving.VanillaFixes
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
