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
            (object emitTracker, IMethodSymbol roslynSymbol) = GetThings(__instance, context);

            bool dontProfile = roslynSymbol.GetAttributes()
                .Any(attr => attr.ToString().Contains("DontUdonProfile", StringComparison.OrdinalIgnoreCase));

            emitTracker.GetType().GetProperty("DefaultValue").SetValue(emitTracker, !dontProfile);
        }

        public static void PostFix(ref object __instance, object context) {
            (object emitTracker, _) = GetThings(__instance, context);
            emitTracker.GetType().GetProperty("DefaultValue").SetValue(emitTracker, false);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> originalCodes = instructions.ToList();
            List<CodeInstruction> codes = new(originalCodes);

            int targetIndex = -1;

            for (int i = 0; i < codes.Count; i++)
                if (codes[i].opcode == OpCodes.Callvirt)
                    if (codes[i].operand.ToString().Contains("LabelJump")) {
                        targetIndex = i + 1;
                        break;
                    }

            if (targetIndex == -1) {
                Injections.PrintError("Failed to find place to inject timing start call");
                return originalCodes;
            }

            MethodInfo targetMethod = typeof(MethodSymbolEmitPatch).GetMethod("InjectStartTiming");
            codes.Insert(targetIndex, new CodeInstruction(OpCodes.Ldarg_0));
            codes.Insert(targetIndex + 1, new CodeInstruction(OpCodes.Ldarg_1));
            codes.Insert(targetIndex + 2, new CodeInstruction(OpCodes.Callvirt, targetMethod));

            return codes;
        }

        public static void InjectStartTiming(object __instance, object context) {
            UdonSharpAssemblyModuleWrapper module = new(context.GetType().GetProperty("Module").GetValue(context));

            object doInject = module.GetValueDefault(UdonProfilerConsts.DoInjectTrackerKey);
            if (doInject is true) {
                (_, IMethodSymbol roslynSymbol) = GetThings(__instance, context);

                module.AddCommentTag("Injected Start Call Begin");

                // Variable for fake self

                object udonTarget = module.GetValue(UdonProfilerConsts.StopwatchSelfKey);

                // Set function name
                module.AddPush(udonTarget);
                module.AddPush(module.GetConstantValue(context, typeof(string), UdonProfilerConsts.StopwatchNameKey));
                module.AddPush(module.GetConstantValue(context, typeof(string),
                    roslynSymbol.ToDisplayString().Replace("\n", "").Replace("\r", "")));
                module.AddExtern(context,
                    "VRCUdonCommonInterfacesIUdonEventReceiver.__SetProgramVariable__SystemString_SystemObject__SystemVoid");

                // Call start profiling
                module.AddPush(udonTarget);
                module.AddPush(module.GetConstantValue(context, typeof(string), "Profiler_StartTiming"));
                module.AddExtern(context,
                    "VRCUdonCommonInterfacesIUdonEventReceiver.__SendCustomEvent__SystemString__SystemVoid");

                module.AddCommentTag("Injected Start Call End");
            }
        }


        private static (object, IMethodSymbol) GetThings(object __instance, object context) {
            Type contextType = context.GetType();
            object rootTable = contextType.GetProperty("RootTable").GetValue(context);
            IList values = (IList)rootTable.GetType().GetProperty("Values").GetValue(rootTable);
            object emitTracker = values.Cast<object>().First(v =>
                (string)v.GetType().GetProperty("UniqueID").GetValue(v) == UdonProfilerConsts.DoInjectTrackerKey);

            Type __instanceType = __instance.GetType().BaseType;

            PropertyInfo[] properties = __instanceType.GetProperties(BindingFlags.Instance | BindingFlags.Public |
                                                                     BindingFlags.NonPublic |
                                                                     BindingFlags.FlattenHierarchy);
            IMethodSymbol roslynSymbol = (IMethodSymbol)properties.FirstOrDefault(property =>
                property.Name == "RoslynSymbol" && property.PropertyType == typeof(IMethodSymbol)).GetValue(__instance);

            return (emitTracker, roslynSymbol);
        }
    }
}