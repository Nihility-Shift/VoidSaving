using Gameplay.Quests;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.CloudSave;

namespace VoidSaving
{
    [HarmonyPatch]
    internal class LoadingPatches
    {
        [HarmonyPatch(typeof(EndlessQuestGenerator), "CreateEndlessQuest"), HarmonyPostfix]
        static void QuestLoadPatch(Quest __result)
        {
            EndlessQuest quest = __result as EndlessQuest;
            if (!SaveHandler.LoadSavedData || quest == null) return;
            {

            }

            SaveGameData saveData = SaveHandler.ActiveData;

            quest.QuestParameters.Seed = saveData.seed;
            quest.JumpCounter = saveData.JumpCounter;
            quest.InterdictionCounter = saveData.InterdictionCounter;
            quest.context.Random = saveData.random;
        }

        [HarmonyPatch(typeof(GameSession), "LoadGameSessionNetworkedAssets"), HarmonyPrefix]
        static void ShipLoadPatch(GameSession __instance)
        {
            __instance.ToLoadShipData = ShipLoadout.FromJObject(SaveHandler.ActiveData.ShipLoadout);
        }
    }
}
