using CG.Game.SpaceObjects.Controllers;
using CG.Ship.Hull;
using CG.Ship.Repair;
using CG.Space;
using Gameplay.Quests;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch]
    internal class LoadingPatches
    {
        [HarmonyPatch(typeof(QuestGenerator), "Create"), HarmonyPostfix]
        static void QuestLoadPatch(Quest __result)
        {
            if (!SaveHandler.LoadSavedData || __result is not EndlessQuest quest) return;

            SaveGameData saveData = SaveHandler.ActiveData;

            quest.QuestParameters.Seed = saveData.seed;
            quest.JumpCounter = saveData.JumpCounter;
            quest.InterdictionCounter = saveData.InterdictionCounter;
            quest.context.Random = saveData.random;
        }

        [HarmonyPatch(typeof(GameSessionManager), "LoadGameSessionNetworkedAssets"), HarmonyPrefix]
        static void ShipLoadPatch(GameSessionManager __instance)
        {
            if (!SaveHandler.LoadSavedData) return;

            __instance.activeGameSession.ToLoadShipData = ShipLoadout.FromJObject(SaveHandler.ActiveData.ShipLoadout);
        }

        [HarmonyPatch(typeof(AbstractPlayerControlledShip), "Start"), HarmonyPostfix]
        static void PostShipLoadPatch(AbstractPlayerControlledShip __instance)
        {
            if (!SaveHandler.LoadSavedData) return;
            SaveGameData activeData = SaveHandler.ActiveData;

            __instance.hitPoints = activeData.ShipHealth;
            HullDamageController HDC = __instance.GetComponentInChildren<HullDamageController>();
            HDC.State.repairableHp = activeData.RepairableShipHealth;
            Helpers.ApplyBreachStatesToBreaches(HDC.breaches, activeData.Breaches);
            Helpers.AddBlueprintsToFabricator(__instance, activeData.UnlockedBPs);
            Helpers.AddRelicsToShip(__instance, activeData.Relics);

            if (activeData.ShipPowered) { __instance.ShipsPowerSystem.PowerOn(); }

            BuildSocketController bsc = __instance.GetComponent<BuildSocketController>();
            int currentValue = 0;
            foreach (BuildSocket socket in bsc.Sockets)
            {
                if (socket.InstalledModule != null && socket.InstalledModule.PowerDrain != null)
                {
                    socket.InstalledModule.PowerDrain.IsOn = activeData.ModulePowerStates[currentValue];
                    currentValue++;
                }
            }

            GameSessionSuppliesManager.Instance.AlloyAmount = activeData.Alloy;
            GameSessionSuppliesManager.Instance.BiomassAmount = activeData.Biomass;

            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.AbstractPlayerShipStart);
        }

        //VJS start puts VJ into inactive. Put into travelling state after load.
        [HarmonyPatch(typeof(VoidJumpSystem), "Start"), HarmonyPostfix]
        static void PostVoidJumpSystemStartPatch(VoidJumpSystem __instance)
        {
            if (!SaveHandler.LoadSavedData) return;

            __instance.DebugTransitionToExitVectorSetState();
            __instance.DebugTransitionToSpinningUpState();
            __instance.DebugTransitionToRotatingState();
            __instance.DebugTransitionToTravellingState();
            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.VoidJumpStart);
        }
    }
}
