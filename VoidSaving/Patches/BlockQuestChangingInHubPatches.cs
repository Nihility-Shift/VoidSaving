using Gameplay.Quests;
using HarmonyLib;

namespace VoidSaving.Patches
{
    [HarmonyPatch(typeof(HubQuestManager))]
    class BlockQuestChangingInHubPatches
    {
        [HarmonyPatch("SelectQuest"), HarmonyPrefix]
        static bool SelectQuestPatch()
        {
            return !SaveHandler.LoadSavedData;
        }

        [HarmonyPatch("SelectShip"), HarmonyPrefix]
        static bool SelectShipPatch()
        {
            return !SaveHandler.LoadSavedData;
        }
    }
}
