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
        internal static Harmony _harmony;
        
        private const string MenuName = "Tools/UdonSharpProfiler/Enabled";

        private static bool _enabled;

        static Injections() {
            _enabled = EditorPrefs.GetBool(MenuName, false);
            
            Toggle(_enabled);
        }

        private static void Unpatch() {
            _harmony.UnpatchAll();
        }

        private static void Patch() {
            _harmony.UnpatchAll();
            _harmony.PatchAll();
            
            var lambdaMethod = typeof(UdonSharpCompilerV1).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                .FirstOrDefault(m => m.Name.Contains("<EmitAllPrograms>b__0"));

            if (lambdaMethod != null) {
                var transpilerMethod = typeof(EmitAllProgramsPatch).GetMethod(nameof(EmitAllProgramsPatch.Transpiler), BindingFlags.Static | BindingFlags.Public);
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
                var transpilerMethod = typeof(CompilePatch).GetMethod(nameof(CompilePatch.Transpiler), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(compileMethod, transpiler: new HarmonyMethod(transpilerMethod));
            }
            
            var getDeclarationStrMethod = ReflectionHelper.GetMethod(ReflectionHelper.ByName("UdonSharp.Compiler.Emit.Value"), "GetDeclarationStr", Type.EmptyTypes, BindingFlags.Public | BindingFlags.Instance);
            if (getDeclarationStrMethod != null) {
                var postfixMethod = typeof(GetDeclarationStrPatch).GetMethod(nameof(GetDeclarationStrPatch.Postfix), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(getDeclarationStrMethod, postfix: new HarmonyMethod(postfixMethod));
            }

            
            var methodSymbolEmitMethod = ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.MethodSymbol")
                .GetMethod("Emit", BindingFlags.Public | BindingFlags.Instance);
            
            if (methodSymbolEmitMethod != null) {
                var prefixMethod = typeof(MethodSymbolEmitPatch).GetMethod(nameof(MethodSymbolEmitPatch.Prefix), BindingFlags.Static | BindingFlags.Public);
                var postfixMethod = typeof(MethodSymbolEmitPatch).GetMethod(nameof(MethodSymbolEmitPatch.PostFix), BindingFlags.Static | BindingFlags.Public);
                var transpileMethod = typeof(MethodSymbolEmitPatch).GetMethod(nameof(MethodSymbolEmitPatch.Transpiler), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(methodSymbolEmitMethod, prefix: new HarmonyMethod(prefixMethod), postfix: new HarmonyMethod(postfixMethod), transpiler: new HarmonyMethod(transpileMethod));
            }
            
            var emitReturnMethod = ReflectionHelper.GetMethod(ReflectionHelper.ByName("UdonSharp.Compiler.Emit.EmitContext"), "EmitReturn", Type.EmptyTypes, BindingFlags.Public | BindingFlags.Instance);
            
            if (emitReturnMethod != null) {
                var prefixMethod = typeof(EmitContextEmitReturnPatch).GetMethod(nameof(EmitContextEmitReturnPatch.Prefix), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(emitReturnMethod, prefix: new HarmonyMethod(prefixMethod));
            }
        }

        [MenuItem(MenuName)]
        public static void ToggleProfiler() {
            Toggle(!_enabled);
            UdonSharpCompilerV1.Compile(new UdonSharpCompileOptions() { IsEditorBuild = true });
        }

        private static void Toggle(bool value) {
            if (_harmony == null)
                _harmony = new Harmony("UdonSharpProfiler.DeltaNeverUsed.patch");
            
            EditorApplication.delayCall += () => { 
                Menu.SetChecked(MenuName, value);
                EditorPrefs.SetBool(MenuName, value);
            };

            _enabled = value;
            
            if (value)
                Patch();
            else
                Unpatch();
        }

        public static void PrintError(object message) {
            Debug.LogError($"<color=red>{new StackFrame(1, true).GetMethod().Name}</color>: {message}");
        }
    }
}
