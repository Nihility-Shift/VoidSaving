using CG.Game.Scenarios;
using Gameplay.Quests;
using HarmonyLib;

namespace VoidSaving.VanillaFixes
{
    //On load state is assigned to started, however if the mission is already failed there's no need for this.
    [HarmonyPatch(typeof(QuestManager), "SetQuestObjectiveState")]
    internal class ObjectiveStateStopStartPatch
    {
        static bool Prefix(Objective objective, ObjectiveState state)
        {
            if (objective.State is ObjectiveState.Completed or ObjectiveState.Failed) return false;
            return true;
        }
    }
}
