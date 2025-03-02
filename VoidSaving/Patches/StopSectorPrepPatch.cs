using HarmonyLib;

namespace VoidSaving.Patches
{
    //Some sector loading is not needed or breaks, so stop early.
    [HarmonyPatch(typeof(GameSessionSector), "CreateTwist")]
    internal class StopSectorPrepPatch
    {
        static bool Prefix()
        {
            return !SaveHandler.LoadSavedData;
        }
    }
}
