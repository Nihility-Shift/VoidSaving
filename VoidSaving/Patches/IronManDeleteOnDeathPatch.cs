using CG.Space;
using HarmonyLib;

namespace VoidSaving.Patches
{
    //Handle here to avoid exploit-based save loss.
    [HarmonyPatch(typeof(AbstractPlayerControlledShip), "Kill")]
    internal class IronManDeleteOnDeathPatch
    {
        static void Postfix()
        {
            SaveHandler.DeleteSaveFile(SaveHandler.LastSaveName);
        }
    }
}
