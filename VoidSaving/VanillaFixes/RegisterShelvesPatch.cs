using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Space;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using static VoidManager.Utilities.HarmonyHelpers;

namespace VoidSaving.VanillaFixes
{
    //Bridge shelves, at least on the destroyer, are not in the core systems list and are not properly saved and loaded by vanilla methods.
    //Adds all shelve CellModules to core systems.
    [HarmonyPatch(typeof(ShipLoadout), "InitializeShip")]
    class RegisterShelvesPatch
    {
        static void PatchMethod(AbstractPlayerControlledShip ship)
        {
            List<CellModule> NewCoreSystems = ship.CoreSystems.ToList();
            foreach (CellModule module in ship.GetComponentsInChildren<CellModule>())
            {
                if (module is CarryablesShelf && !ship.CoreSystems.Contains(module))
                {
                    if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Adding {module.DisplayName} with to core systems");
                    NewCoreSystems.Add(module);
                }
            }

            ship.CoreSystems = NewCoreSystems.ToArray();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] targetSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Stloc_2)
            };

            CodeInstruction[] patchSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RegisterShelvesPatch), "PatchMethod"))
            };

            return PatchBySequence(instructions, targetSequence, patchSequence, PatchMode.AFTER, CheckMode.NEVER);
        }
    }
}
