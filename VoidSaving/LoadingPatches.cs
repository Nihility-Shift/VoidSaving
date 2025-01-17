﻿using CG.Game.SpaceObjects.Controllers;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Repair;
using CG.Space;
using Client.Utils;
using Gameplay.Quests;
using HarmonyLib;

namespace VoidSaving
{
    [HarmonyPatch]
    internal class LoadingPatches
    {
        [HarmonyPatch(typeof(Quest), "NextJumpInterdictionCheck")]
        internal class CapturePreJumpPatch
        {
            static void Prefix(Quest __instance)
            {
                if (__instance is not EndlessQuest endlessQuest) return;

                if (SaveHandler.LoadSavedData)
                {
                    SaveGameData activeData = SaveHandler.ActiveData;

                    SaveHandler.LatestRandom = activeData.Random;
                    endlessQuest.context.Random = activeData.Random.DeepCopy();

                    SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.QuestDataRandomSet);
                }
                else
                {
                    //Capture current random and quest data for saving prior to generation of next section.
                    SaveHandler.LatestRandom = endlessQuest.Context.Random.DeepCopy();
                    BepinPlugin.Log.LogInfo($"LatestRandom: {SaveHandler.LatestRandom.Next()}; CurrantRandom: {endlessQuest.Context.Random.Next()}");
                    SaveHandler.LatestCurrentInterdictionChance = endlessQuest.CurrentInterdictionChance;

                }
            }
        }

        [HarmonyPatch(typeof(EndlessQuest), "GenerateNextSection"), HarmonyPrefix]
        static void SectionDataLoadPatch(EndlessQuest __instance)
        {
            if (SaveHandler.LoadSavedData)
            {
                SaveGameData activeData = SaveHandler.ActiveData;

                __instance.context.NextSectionParameters.NextSectorId = activeData.NextSectorID;

                __instance.context.ActiveSolarSystemIndex = activeData.ActiveSolarSystemID;
                __instance.context.NextSectionParameters.SolarSystem = __instance.parameters.SolarSystems[activeData.ActiveSolarSystemID];

                __instance.context.NextSolarSystemIndex = activeData.NextSolarSystemID;
                __instance.context.NextSectionParameters.SectionIndex = activeData.NextSectionIndex;
                __instance.context.NextSectionParameters.EnemyLevelRange.Min = activeData.EnemyLevelRangeMin;
                __instance.context.NextSectionParameters.EnemyLevelRange.Max = activeData.EnemyLevelRangeMax;
                __instance.context.SectorsUsedInSolarSystem = activeData.SectorsUsedInSolarSystem;
                __instance.context.SectorsToUseInSolarSystem = activeData.SectorsToUseInSolarSystem;
            }
            else
            {
                SaveHandler.LatestNextSectorID = __instance.context.NextSectionParameters.NextSectorId;
                SaveHandler.LatestActiveSolarSystemID = __instance.context.ActiveSolarSystemIndex;
                SaveHandler.LatestNextSolarSystemID = __instance.context.NextSolarSystemIndex;
                SaveHandler.LatestSectionIndex = __instance.context.NextSectionParameters.SectionIndex;
                SaveHandler.LatestEnemyLevelMin = __instance.context.NextSectionParameters.EnemyLevelRange.Min;
                SaveHandler.LatestEnemyLevelMax = __instance.context.NextSectionParameters.EnemyLevelRange.Max;
                SaveHandler.LatestSectorsUsedInSolarSystem = __instance.context.SectorsUsedInSolarSystem;
                SaveHandler.LatestSectorsToUseInSolarSystem = __instance.context.SectorsToUseInSolarSystem;
            }
        }


        /*
        //Sets quest data prior to generation of next section
        static void SetQuestDataPatchMethod(EndlessQuest Instance)
        {
            if (SaveHandler.LoadSavedData)
            {
                SaveGameData activeData = SaveHandler.ActiveData;


                SaveHandler.LatestRandom = activeData.Random;
                Instance.context.Random = activeData.Random.DeepCopy();

                Instance.context.NextSectionParameters.NextSectorId = activeData.NextSectorID;

                Instance.context.ActiveSolarSystemIndex = activeData.ActiveSolarSystemID;
                Instance.context.NextSectionParameters.SolarSystem = Instance.parameters.SolarSystems[activeData.ActiveSolarSystemID];

                Instance.context.NextSolarSystemIndex = activeData.NextSolarSystemID;
                Instance.context.NextSectionParameters.SectionIndex = activeData.NextSectionIndex;
                Instance.context.NextSectionParameters.EnemyLevelRange.Min = activeData.EnemyLevelRangeMin;
                Instance.context.NextSectionParameters.EnemyLevelRange.Max = activeData.EnemyLevelRangeMax;

                SaveHandler.CompleteLoadingStage(SaveHandler.LoadingStage.QuestDataRandomSet);
            }
        }

        [HarmonyPatch(typeof(EndlessQuest), "GenerateStartingSection"), HarmonyTranspiler]
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
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoadingPatches), "SetQuestDataPatchMethod")),
            };

            return PatchBySequence(instructions, targetSequence, patchSequence, PatchMode.BEFORE, CheckMode.NONNULL);
        }*/

        //Sets seed at earliest point
        [HarmonyPatch(typeof(QuestGenerator), "Create"), HarmonyPostfix]
        static void QuestLoadPrefix(QuestParameters questParameters)
        {
            if (!SaveHandler.LoadSavedData) return;

            questParameters.Seed = SaveHandler.ActiveData.Seed;
        }

        //Sets jump and interdiction counters prior to first usage
        [HarmonyPatch(typeof(QuestGenerator), "Create"), HarmonyPostfix]
        static void QuestLoadPostfix(Quest __result)
        {
            if (!SaveHandler.LoadSavedData || __result is not EndlessQuest quest) return;

            //Load JumpCounter prior to jump to keep Interdiction chance the same.
            quest.JumpCounter = SaveHandler.ActiveData.JumpCounter - 1;
            quest.InterdictionCounter = SaveHandler.ActiveData.InterdictionCounter;
        }

        //Loads ship from vanilla ship data save/load system
        [HarmonyPatch(typeof(GameSessionManager), "LoadGameSessionNetworkedAssets"), HarmonyPrefix]
        static void ShipLoadPatch(GameSessionManager __instance)
        {
            if (!SaveHandler.LoadSavedData) return;

            __instance.activeGameSession.ToLoadShipData = ShipLoadout.FromJObject(SaveHandler.ActiveData.ShipLoadout);
        }

        //loads various ship data at start.
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
