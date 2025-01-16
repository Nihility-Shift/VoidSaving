using CG.Ship.Hull;
using CG.Ship.Modules;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using static VoidManager.Utilities.HarmonyHelpers;

namespace VoidSaving
{
    //Ship Load attempts to utilize carryable socket provider's carryables sockets, but these are not available on ship load.
    //Attempt to replace with connected sockets, which should get added to by individual mod slot components on awake.
    [HarmonyPatch]
    class ModuleSocketsLoadPatches
    {
        static CodeInstruction[] targetSequence = new CodeInstruction[] { new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(CellModule), "CarryablesSockets")) };
        static CodeInstruction[] patchSequence = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CellModule), "ConnectedSockets")) };

        //Helper code which is re-used.
        static IEnumerable<CodeInstruction> PatchCarryableSockets(IEnumerable<CodeInstruction> instructions)
        {
            return PatchBySequence(instructions, targetSequence, patchSequence, PatchMode.REPLACE);
        }

        //Gravity Scoops detect carryables differently, and need an exception made for proper loading.
        //Add Carryables Sockets list for Gravity scoops
        static void MSLPatchMethod(CellModule module, List<CarryablesSocket> sockets)
        {
            if (sockets.Count == 0 && module is GravityScoopModule GSModule)
            {
                BepinPlugin.Log.LogInfo($"Attempting to patch GravScoop. {GSModule.CarryablesSockets.Count} sockets found.");
                sockets.AddRange(GSModule.CarryablesSockets);
            }
        }

        [HarmonyPatch(typeof(ShipLoadout), "InitializeShip"), HarmonyTranspiler]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified", Justification = "N/A")]
        static IEnumerable<CodeInstruction> ModuleSocketsLoadPatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] tSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Brfalse),
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Call),
                new CodeInstruction(OpCodes.Stloc_S)
            };

            CodeInstruction[] pSequence = new CodeInstruction[] 
            { 
                new CodeInstruction(OpCodes.Ldloc_S, (byte)8), 
                new CodeInstruction(OpCodes.Ldloc_S, (byte)13), 
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModuleSocketsLoadPatches), "MSLPatchMethod")) 
            };

            instructions = PatchBySequence(instructions, tSequence, pSequence, PatchMode.AFTER, CheckMode.NONNULL);

            //Run 2 times
            return PatchCarryableSockets(PatchCarryableSockets(instructions));
        }

        [HarmonyPatch(typeof(ShipLoadout), "LoadSocketData"), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ModuleSocketsSavePatch(IEnumerable<CodeInstruction> instructions)
        {
            return PatchCarryableSockets(instructions);
        }

        //Core systems break when loading, and it isn't needed right now.
        /*[HarmonyPatch(typeof(ShipLoadout), "LoadCoreSystemData")]
        static IEnumerable<CodeInstruction> CoreSystemSocketsSavePatch(IEnumerable<CodeInstruction> instructions)
        {
            return PatchCarryableSockets(instructions);
        }*/
    }
}
