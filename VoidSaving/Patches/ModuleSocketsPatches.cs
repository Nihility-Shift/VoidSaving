using CG.Game;
using CG.Objects;
using CG.Ship.Hull;
using CG.Ship.Modules;
using CG.Ship.Object;
using HarmonyLib;
using ResourceAssets;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using static VoidManager.Utilities.HarmonyHelpers;

namespace VoidSaving.Patches
{
    //Ship Load attempts to utilize carryable socket provider's carryables sockets, but these are not available on ship load.
    //Attempt to replace with connected sockets, which should get added to by individual mod slot components on awake.
    //Also loading/saving ammo and power containers
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
            if (sockets.Count == 0 && module is GravityScoopModule or ChargeStationModule)
            {
                if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Attempting to patch GravScoop or batteryCharger. {module.CarryablesSockets.Count} sockets found.");
                sockets.AddRange(module.CarryablesSockets);
            }
        }


        //Assigns space platform (prevents objects getting deleted on loading jump)
        //Run as carryables are loade, utilizing resource values from active data.
        static void LoadResourceContainers(CarryableObject carryable)
        {
            if (!SaveHandler.LoadSavedData) return;

            carryable.SetPlatform(ClientGame.Current.PlayerShip.Platform);

            if (carryable is ResourceContainer resourceContainer)
            {
                if (resourceContainer is PowerResourceContainer)
                {
                    resourceContainer.amount = SaveHandler.ActiveData.PowerResourceValues[0];
                    SaveHandler.ActiveData.PowerResourceValues.RemoveAt(0);
                }
                else if (resourceContainer is AmmoContainer)
                {
                    resourceContainer.amount = SaveHandler.ActiveData.AmmoResourceValues[0];
                    SaveHandler.ActiveData.AmmoResourceValues.RemoveAt(0);
                }
            }
        }

        [HarmonyPatch(typeof(ShipLoadout), "InitializeShip"), HarmonyTranspiler]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified", Justification = "N/A")]
        static IEnumerable<CodeInstruction> ModuleSocketsLoadPatch(IEnumerable<CodeInstruction> instructions)
        {
            //Load Power and Ammo Resource Container values as carryables load
            CodeInstruction[] resourceContainersTargetSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CarryablesSocket), "TryInsertCarryable")),
                new CodeInstruction(OpCodes.Pop),
            };

            CodeInstruction[] resourceContainersPatchSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S, (byte)16),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModuleSocketsLoadPatches), "LoadResourceContainers"))
            };

            instructions = PatchBySequence(instructions, resourceContainersTargetSequence, resourceContainersPatchSequence, PatchMode.AFTER, CheckMode.NONNULL);

            //Targetting core system payload objects
            resourceContainersTargetSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Call),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Stloc_S),
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CarryablesSocket), "TryInsertCarryable")),
                new CodeInstruction(OpCodes.Pop),
            };

            resourceContainersPatchSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S, (byte)22),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModuleSocketsLoadPatches), "LoadResourceContainers"))
            };

            instructions = PatchBySequence(instructions, resourceContainersTargetSequence, resourceContainersPatchSequence, PatchMode.AFTER, CheckMode.NONNULL);

            //Targetting additional assets. Only carryable object getter which doesn't utilize the return value (devs probably forgot to delete the line, as it does nothing.)
            resourceContainersTargetSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "GetComponent", null, new Type[] { typeof(CarryableObject) })),
                new CodeInstruction(OpCodes.Pop),
            };

            resourceContainersPatchSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "GetComponent", null, new Type[] { typeof(CarryableObject) })),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModuleSocketsLoadPatches), "LoadResourceContainers"))
            };

            instructions = PatchBySequence(instructions, resourceContainersTargetSequence, resourceContainersPatchSequence, PatchMode.REPLACE, CheckMode.NONNULL);



            //Fix gravity scoop loading
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

            //Patch/replace first two CarryableSocket properties with field read.
            return PatchCarryableSockets(PatchCarryableSockets(instructions));
        }


        //collects battery and ammo can data.
        static void SaveModuleResourceContainers(List<CarryablesSocket> Sockets, int j)
        {
            if (Sockets[j].Payload is ResourceContainer Container)
            {
                if (Container is PowerResourceContainer)
                {
                    SaveHandler.LatestData.PowerResourceValues.Add(Container.Amount);
                }
                else if (Container is AmmoContainer)
                {
                    SaveHandler.LatestData.AmmoResourceValues.Add(Container.Amount);
                }
            }
        }

        // Collects list and current index for processing payloads
        [HarmonyPatch(typeof(ShipLoadout), "LoadSocketData"), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ModuleSocketsSavePatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] tSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ResourceAssetDef<GameObject>), "Ref")),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(ShipAssetRef), new Type[] { typeof(ResourceAssetRef) })),
                new CodeInstruction(OpCodes.Stelem_Ref),
            };

            CodeInstruction[] pSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S, (byte)5),
                new CodeInstruction(OpCodes.Ldloc_S, (byte)6),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModuleSocketsLoadPatches), "SaveModuleResourceContainers"))
            };

            return PatchBySequence(PatchCarryableSockets(instructions), tSequence, pSequence, PatchMode.AFTER, CheckMode.NONNULL);
        }

        // PatchCarryableSockets does not work here, and when I attempted using it broke things.
        // Collects list and current index for processing payloads
        [HarmonyPatch(typeof(ShipLoadout), "LoadCoreSystemData"), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CoreSystemSocketsSavePatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] tSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Ldloc_S),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ResourceAssetDef<GameObject>), "Ref")),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(ShipAssetRef), new Type[] { typeof(ResourceAssetRef) })),
                new CodeInstruction(OpCodes.Stloc_S),
            };

            CodeInstruction[] pSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S, (byte)5),
                new CodeInstruction(OpCodes.Ldloc_S, (byte)6),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModuleSocketsLoadPatches), "SaveModuleResourceContainers"))
            };
            return PatchBySequence(instructions, tSequence, pSequence, PatchMode.AFTER, CheckMode.NONNULL);
        }


        //collects battery and ammo can data from additional assets (loose carryables)
        static void SLRCPatchMethod(CarryableObject carryable)
        {
            if (carryable is ResourceContainer Container)
            {
                if (Container is PowerResourceContainer)
                {
                    SaveHandler.LatestData.PowerResourceValues.Add(Container.Amount);
                }
                else if (Container is AmmoContainer)
                {
                    SaveHandler.LatestData.AmmoResourceValues.Add(Container.Amount);
                }
            }
        }

        [HarmonyPatch(typeof(ShipLoadout), "LoadAdditionalAssetData"), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SaveLooseResourceContainers(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] tSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<GameSessionAsset>), "Add")),
            };

            CodeInstruction[] pSequence = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModuleSocketsLoadPatches), "SLRCPatchMethod"))
            };

            return PatchBySequence(instructions, tSequence, pSequence, PatchMode.AFTER);
        }
    }
}
