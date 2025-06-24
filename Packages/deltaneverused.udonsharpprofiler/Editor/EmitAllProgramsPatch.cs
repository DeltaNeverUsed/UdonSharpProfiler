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
        /// <summary>
        ///     Creates a new Udon Variable
        /// </summary>
        /// <returns>Code to inject</returns>
        private static List<CodeInstruction> AddUdonReflVars(string key, Type instanceType, bool createNewInstance) {
            Type emitContextType = ReflectionHelper.ByName("UdonSharp.Compiler.Emit.EmitContext");
            Type valueTableType = ReflectionHelper.ByName("UdonSharp.Compiler.Emit.ValueTable");
            Type phaseContextType = ReflectionHelper.ByName("UdonSharp.Compiler.AbstractPhaseContext");
            Type typeSymbolType = ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.TypeSymbol");

            List<CodeInstruction> codeList = new() {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Callvirt, emitContextType.GetProperty("RootTable").GetGetMethod()),
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

            return codeList;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> originalCodes = instructions.ToList();
            List<CodeInstruction> codes = new(originalCodes);
            int paramInsertIndex = -1;

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode != OpCodes.Ldstr) continue;
                if (codes[i].operand.ToString() != "__refl_typename") continue;

                for (int j = i; j < codes.Count; j++)
                    if (codes[j].opcode == OpCodes.Pop) {
                        paramInsertIndex = j + 1;
                        break;
                    }

                break;
            }

            if (paramInsertIndex == -1) {
                Injections.PrintError("Couldn't find CreateReflectionValue location");
                return originalCodes;
            }

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