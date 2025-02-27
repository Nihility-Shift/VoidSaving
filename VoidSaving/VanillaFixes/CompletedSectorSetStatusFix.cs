using CG.Game.Scenarios;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using static VoidManager.Utilities.HarmonyHelpers;

namespace VoidSaving.VanillaFixes
{
    //When 'CompleteSector' is called, the mission status is not properly updated.
    [HarmonyPatch(typeof(EndlessQuestManager), "CompleteSector")]
    internal class CompletedSectorSetStatusFix
    {
        static void PatchMethod(EndlessQuestManager instance, byte status)
        {
            GameSessionSector sector = instance.endlessQuest.Context.CompletedSectors.Last();
            if (sector.SectorObjective != null)
            {
                switch((SectorCompletionStatus)status)
                {
                    case SectorCompletionStatus.Completed:
                        sector.SectorObjective.Objective.State = ObjectiveState.Completed;
                        break;
                    case SectorCompletionStatus.Failed:
                        sector.SectorObjective.Objective.State = ObjectiveState.Failed;
                        break;
                    case SectorCompletionStatus.NothingToDo:
                        sector.SectorObjective.Objective.State = ObjectiveState.NoObjective;
                        break;
                }
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] targetSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EndlessQuestManager), "RefreshSectorVisibility"))
            };

            CodeInstruction[] patchSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompletedSectorSetStatusFix), "PatchMethod"))
            };

            return PatchBySequence(instructions, targetSequence, patchSequence, PatchMode.BEFORE, CheckMode.NONNULL);
        }
    }
}
