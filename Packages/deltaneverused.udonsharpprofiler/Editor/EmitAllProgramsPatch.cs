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
        /// Creates a new Udon Variable
        /// </summary>
        /// <returns>Code to inject</returns>
        private static List<CodeInstruction> AddUdonReflVars(string key, Type instanceType, bool createNewInstance) {
            var emitContextType = ReflectionHelper.ByName("UdonSharp.Compiler.Emit.EmitContext");
            var valueTableType = ReflectionHelper.ByName("UdonSharp.Compiler.Emit.ValueTable");
            var phaseContextType = ReflectionHelper.ByName("UdonSharp.Compiler.AbstractPhaseContext");
            var typeSymbolType = ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.TypeSymbol");

            var codeList = new List<CodeInstruction> {
                new(OpCodes.Ldloc_3),
                new(OpCodes.Callvirt                        , emitContextType.GetProperty("RootTable").GetGetMethod()),
                new(OpCodes.Ldstr                           , key),
                new(OpCodes.Ldloc_3),
                new(OpCodes.Ldtoken                         , instanceType),
                new(OpCodes.Call                            , typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) })),
                new(OpCodes.Callvirt, ReflectionHelper.GetMethod(phaseContextType, "GetTypeSymbol", new[] { typeof(Type) }, BindingFlags.Public | BindingFlags.Instance)),
                createNewInstance // Create new instance or null
                    ? new CodeInstruction(OpCodes.Newobj    , ReflectionHelper.GetConstructor(instanceType, Type.EmptyTypes, BindingFlags.Public | BindingFlags.Instance))
                    : new CodeInstruction(OpCodes.Ldnull)   ,
                new(OpCodes.Callvirt                        , ReflectionHelper.GetMethod(valueTableType, "CreateReflectionValue", new[] { typeof(string), typeSymbolType, typeof(object) }, BindingFlags.Public | BindingFlags.Instance)),
                new(OpCodes.Pop)
            };

            return codeList;
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
                AddUdonReflVars(UdonProfilerConsts.StopwatchListKey, typeof(DataList), true)
            );
            
            codes.InsertRange(
                paramInsertIndex,
                AddUdonReflVars(UdonProfilerConsts.StopwatchParentKey, typeof(DataDictionary), false)
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