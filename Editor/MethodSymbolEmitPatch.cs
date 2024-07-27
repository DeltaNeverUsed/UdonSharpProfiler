using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.CodeAnalysis;

namespace UdonSharpProfiler {
    public static class MethodSymbolEmitPatch {
        public static void Prefix(ref object __instance, object context) {
            var (emitTracker, roslynSymbol) = GetThings(__instance, context);

            var dontProfile = roslynSymbol.GetAttributes()
                .Any(attr => attr.ToString().Contains("DontUdonProfile", StringComparison.OrdinalIgnoreCase));

            emitTracker.GetType().GetProperty("DefaultValue").SetValue(emitTracker, !dontProfile);
        }

        public static void PostFix(ref object __instance, object context) {
            var (emitTracker, _) = GetThings(__instance, context);
            emitTracker.GetType().GetProperty("DefaultValue").SetValue(emitTracker, false);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var originalCodes = instructions.ToList();
            var codes = new List<CodeInstruction>(originalCodes);

            var targetIndex = -1;

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Callvirt) {
                    if (codes[i].operand.ToString().Contains("LabelJump")) {
                        targetIndex = i + 1;
                        break;
                    }
                }
            }

            if (targetIndex == -1) {
                Injections.PrintError("Failed to find place to inject timing start call");
                return originalCodes;
            }

            var targetMethod = typeof(MethodSymbolEmitPatch).GetMethod("InjectStartTiming");
            codes.Insert(targetIndex         , new(OpCodes.Ldarg_0));
            codes.Insert(targetIndex + 1, new(OpCodes.Ldarg_1));
            codes.Insert(targetIndex + 2, new(OpCodes.Callvirt, targetMethod));

            return codes;
        }

        public static void InjectStartTiming(object __instance, object context) {
            var module = new UdonSharpAssemblyModuleWrapper(context.GetType().GetProperty("Module").GetValue(context));

            var doInject = module.GetValueDefault(UdonProfilerConsts.DoInjectTrackerKey);
            if (doInject is true) {
                var (_, roslynSymbol) = GetThings(__instance, context);

                module.AddCommentTag("Injected Start Call Begin");

                // Variable for fake self

                var udonTarget = module.GetValue(UdonProfilerConsts.StopwatchSelfKey);

                // Set function name
                module.AddPush(udonTarget);
                module.AddPush(module.GetConstantValue(context, typeof(string), UdonProfilerConsts.StopwatchNameKey));
                module.AddPush(module.GetConstantValue(context, typeof(string), roslynSymbol.ToDisplayString().Replace("\n", "").Replace("\r", "")));
                module.AddExtern(context                                      , "VRCUdonCommonInterfacesIUdonEventReceiver.__SetProgramVariable__SystemString_SystemObject__SystemVoid");

                // Call start profiling
                module.AddPush(udonTarget);
                module.AddPush(module.GetConstantValue(context, typeof(string), "Profiler_StartTiming"));
                module.AddExtern(context                                      , "VRCUdonCommonInterfacesIUdonEventReceiver.__SendCustomEvent__SystemString__SystemVoid");

                module.AddCommentTag("Injected Start Call End");
            }
        }


        private static (object, IMethodSymbol) GetThings(object __instance, object context) {
            var contextType = context.GetType();
            var rootTable = contextType.GetProperty("RootTable").GetValue(context);
            var values = (IList)rootTable.GetType().GetProperty("Values").GetValue(rootTable);
            var emitTracker = values.Cast<object>().First(v =>
                (string)v.GetType().GetProperty("UniqueID").GetValue(v) == UdonProfilerConsts.DoInjectTrackerKey);

            var __instanceType = __instance.GetType().BaseType;

            var properties = __instanceType.GetProperties(BindingFlags.Instance | BindingFlags.Public |
                                                          BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            var roslynSymbol = (IMethodSymbol)properties.FirstOrDefault(property =>
                property.Name == "RoslynSymbol" && property.PropertyType == typeof(IMethodSymbol)).GetValue(__instance);

            return (emitTracker, roslynSymbol);
        }
    }
}