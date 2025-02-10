using CG.Game.Scenarios;
using Gameplay.Quests;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UI.AstralMap;

namespace VoidSaving.VanillaFixes
{
    //Astral map loads all completed sections of the current solar system on load, however due to looping though solar systems and solar system tracking the astral map
    //fetches data from multiple systems. When this data is loaded and opened by a player (usually only a bug for new players in MP, but now a bug for everyone using void saving),
    //the astral map loads more than 5 sections when it expects less than 5.

    //Code is more or less a copy, with fixes for this bug.
    [HarmonyPatch(typeof(AstralMapController), "FetchCurrentSystemProgression")]
    internal class AstralMapFetchProgressionFix
    {
        static bool Prefix(AstralMapController __instance)
        {
            EndlessQuest endlessQuest = GameSessionManager.ActiveSession.ActiveQuest as EndlessQuest;
            if (endlessQuest == null)
            {
                return false;
            }
            __instance._newSystem = endlessQuest.Context.CurrentSection.SolarSystem != endlessQuest.Context.NextSection.SolarSystem;
            List<UIObjective> list = new List<UIObjective>();
            List<GameSessionSection> CompletedSections = endlessQuest.Context.CompletedSections;

            //New Code - Changed from foreach loop to for loop, focusing on last 5/6 sections. This stops creation of progression history items for old sections and avoids looping through too many times.
            int length = CompletedSections.Count;
            for (int i = Math.Max(length - 6, 0); i < length; i++)
            {
                GameSessionSection CurrentSection = CompletedSections[i];
                if (CurrentSection.SolarSystem == endlessQuest.Context.CurrentSection.SolarSystem)
                {
                    foreach (GameSessionSector gameSessionSector in CurrentSection.AllAvailableSectors)
                    {
                        ObjectiveState objectiveState = gameSessionSector.ObjectiveState;
                        if (objectiveState == ObjectiveState.Completed || objectiveState == ObjectiveState.Failed || objectiveState == ObjectiveState.Started)
                        {
                            list.Add(__instance.CreateProgressionHistoryItem(CurrentSection, gameSessionSector));
                        }
                    }
                }
            }
            foreach (GameSessionSector gameSessionSector2 in endlessQuest.Context.CurrentSection.AllAvailableSectors)
            {
                ObjectiveState objectiveState = gameSessionSector2.ObjectiveState;
                if (objectiveState == ObjectiveState.Completed || objectiveState == ObjectiveState.Failed || objectiveState == ObjectiveState.Started)
                {
                    list.Add(__instance.CreateProgressionHistoryItem(endlessQuest.Context.CurrentSection, gameSessionSector2));
                }
            }
            if (GameSessionManager.ActiveSector == null)
            {
                if (__instance._voidJumpSystem.DestinationSector != null)
                {
                    list.Add(__instance.CreateProgressionHistoryItem(endlessQuest.Context.CurrentSection, __instance._voidJumpSystem.DestinationSector));
                }
                else
                {
                    list.Add(null);
                }
            }
            if (list.Count > 0)
            {
                __instance._currentProgressionHistory = list;
            }
            return false;
        }
    }
}
