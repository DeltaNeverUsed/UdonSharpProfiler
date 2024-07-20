using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UdonSharp.Compiler;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace UdonSharpProfiler {
    [InitializeOnLoad]
    public static class Injections {
        public static Assembly UdonSharpAssembly = typeof(UdonSharpCompilerV1).Assembly;

        internal static Harmony _harmony;

        static Injections() {
            
            var readMethod = UdonSharpAssembly.GetType("UdonSharp.UdonSharpUtils").GetMethod("ReadFileTextSync");
            
            if (_harmony == null)
                _harmony = new Harmony("UdonSharpProfiler.DeltaNeverUsed.patch");
            
            _harmony.UnpatchAll();
            _harmony.PatchAll();
            
            var lambdaMethod = typeof(UdonSharpCompilerV1).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                .FirstOrDefault(m => m.Name.Contains("<EmitAllPrograms>b__0"));

            if (lambdaMethod != null) {
                var transpilerMethod = typeof(EmitAllProgramsPatch).GetMethod("Transpiler", BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(lambdaMethod, transpiler: new HarmonyMethod(transpilerMethod));
            }
            
            
            var compileMethodParameters = new Type[] {
                ReflectionHelper.ByName("UdonSharp.Compiler.CompilationContext"),
                typeof(IReadOnlyDictionary<,>).MakeGenericType(typeof(string), ReflectionHelper.ByName("UdonSharp.Compiler.UdonSharpCompilerV1/ProgramAssetInfo")),
                typeof(IEnumerable<string>),
                typeof(string[])
            };
            
            var compileMethod = typeof(UdonSharpCompilerV1).GetMethod("Compile", BindingFlags.NonPublic | BindingFlags.Static, null, compileMethodParameters, null);
            if (compileMethod != null) {
                var transpilerMethod = typeof(CompilePatch).GetMethod("Transpiler", BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(compileMethod, transpiler: new HarmonyMethod(transpilerMethod));
            }
        }

        public static void PrintError(object message) {
            Debug.LogError($"<color=red>{new StackFrame(1, true).GetMethod().Name}</color>: {message}");
        }
    }
}
