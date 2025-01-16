﻿using CG.Game.SpaceObjects.Controllers;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Repair;
using CG.Space;
using Gameplay.Quests;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch]
    internal class LoadingPatches
    {
        //Sets Random prior to generation of next section
        static void SetRandomPatchMethod(EndlessQuest Instance)
        {
            SaveHandler.LatestRandom = SaveHandler.ActiveData.random;
            Instance.context.Random = SaveHandler.ActiveData.random.DeepCopy();
            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.QuestDataRandomSet);
        }

        [HarmonyPatch(typeof(EndlessQuest), MethodType.Constructor), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> EndlessQuestLoadPatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] targetSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call),
                new CodeInstruction(OpCodes.Ret),
            };

            CodeInstruction[] patchSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoadingPatches), "SetRandomPatchMethod")),
            };

            return PatchBySequence(instructions, targetSequence, patchSequence, PatchMode.BEFORE, CheckMode.NONNULL);
        }
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

            int currentValue = 0;
            foreach (CellModule module in __instance.CoreSystems)
            {
                if (module != null && module.PowerDrain != null)
                {
                    if (activeData.ShipSystemPowerStates[currentValue])
                    {
                        module.TurnOn();
                    }
                    currentValue++;
                }
            }

            BuildSocketController bsc = __instance.GetComponent<BuildSocketController>();
            currentValue = 0;
            foreach (BuildSocket socket in bsc.Sockets)
            {
                if (socket.InstalledModule != null && socket.InstalledModule.PowerDrain != null)
                {
                    if (activeData.ModulePowerStates[currentValue])
                    {
                        socket.InstalledModule.TurnOn();
                    }
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
            __instance.DebugTransitionToRotatingState();
            __instance.DebugTransitionToSpinningUpState();
            __instance.DebugTransitionToTravellingState();
            SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.VoidJumpStart);
        }
    }
}
