using CG.Client.Quests.SectorTwists;
using HarmonyLib;

namespace VoidSaving.Patches
{
    //AstroidRain tries to read the player ship, which doesn't exist during early load. Prevent execution if loading.
    [HarmonyPatch(typeof(AsteroidRain), "SetRandomSpawnDirection")]
    internal class AstroidRainLoadExceptionPatch
    {
        static bool Prefix()
        {
            return !SaveHandler.LoadSavedData;
        }
    }
}
