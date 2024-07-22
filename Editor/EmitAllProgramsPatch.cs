using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UdonSharp;
using VRC.SDK3.Data;

namespace UdonSharpProfiler {
    public static class EmitAllProgramsPatch {
        private static List<CodeInstruction> AddUdonReflVars(string key, Type instanceType, bool createNewInstance) {
            var emitContextType = ReflectionHelper.ByName("UdonSharp.Compiler.Emit.EmitContext");
            var valueTableType = ReflectionHelper.ByName("UdonSharp.Compiler.Emit.ValueTable");
            var phaseContextType = ReflectionHelper.ByName("UdonSharp.Compiler.AbstractPhaseContext");
            var typeSymbolType = ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.TypeSymbol");

            var codeList = new List<CodeInstruction> {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Callvirt,
                    emitContextType.GetProperty("RootTable").GetGetMethod()),
                new CodeInstruction(OpCodes.Ldstr, key),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldtoken, instanceType),
                new CodeInstruction(OpCodes.Call,
                    typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) })),
                new CodeInstruction(OpCodes.Callvirt,
                    ReflectionHelper.GetMethod(phaseContextType, "GetTypeSymbol", new[] { typeof(Type) },
                        BindingFlags.Public | BindingFlags.Instance)),
                createNewInstance // Create new instance or null
                    ? new CodeInstruction(OpCodes.Newobj,
                        ReflectionHelper.GetConstructor(instanceType, Type.EmptyTypes,
                            BindingFlags.Public | BindingFlags.Instance))
                    : new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Callvirt,
                    ReflectionHelper.GetMethod(valueTableType, "CreateReflectionValue",
                        new[] { typeof(string), typeSymbolType, typeof(object) },
                        BindingFlags.Public | BindingFlags.Instance)),
                new CodeInstruction(OpCodes.Pop)
            };

            return codeList; //
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var originalCodes = instructions.ToList();
            var codes = new List<CodeInstruction>(originalCodes);
            var paramInsertIndex = -1;

            for (var i = 0; i < codes.Count; i++) {
                if (codes[i].opcode != OpCodes.Ldstr) continue;
                if (codes[i].operand.ToString() != "__refl_typename") continue;

                for (int j = i; j < codes.Count; j++) {
                    if (codes[j].opcode == OpCodes.Pop) {
                        paramInsertIndex = j + 1;
                        break;
                    }
                }

                break;
            }

            if (paramInsertIndex == -1) {
                Injections.PrintError("Couldn't find CreateReflectionValue location");
                return originalCodes;
            }

            codes.InsertRange(
                paramInsertIndex,
                AddUdonReflVars(UdonProfilerConsts.StopwatchHeapKey, typeof(DataDictionary), true)
            );

            codes.InsertRange(
                paramInsertIndex,
                AddUdonReflVars(UdonProfilerConsts.StopwatchHeapParentKey, typeof(DataDictionary), false)
            );
            
            codes.InsertRange(
                paramInsertIndex,
                AddUdonReflVars(UdonProfilerConsts.StopwatchSelfKey, typeof(UdonSharpBehaviour), false)
            );
            
            codes.InsertRange(
                paramInsertIndex,
                AddUdonReflVars(UdonProfilerConsts.StopwatchNameKey, typeof(string), false)
            );
            
            codes.InsertRange(
                paramInsertIndex,
                AddUdonReflVars(UdonProfilerConsts.DoInjectTrackerKey, typeof(bool), false)
            );

            return codes.AsEnumerable();
        }
    }
}