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
        private const string MenuName = "Tools/UdonSharpProfiler/Enabled";
        private static Harmony _harmony;

        private static bool _enabled;

        static Injections() {
            _enabled = EditorPrefs.GetBool(MenuName, false);

            Toggle(_enabled);
        }

        private static void Unpatch() {
            _harmony.UnpatchAll("UdonSharpProfiler.DeltaNeverUsed.patch");
        }

        private static void Patch() {
            _harmony.UnpatchAll("UdonSharpProfiler.DeltaNeverUsed.patch");
            _harmony.PatchAll();


            MethodInfo lambdaMethod = typeof(UdonSharpCompilerV1)
                .GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                .FirstOrDefault(m => m.Name.Contains("<EmitAllPrograms>b__0"));

            if (lambdaMethod != null) {
                MethodInfo transpilerMethod =
                    typeof(EmitAllProgramsPatch).GetMethod(nameof(EmitAllProgramsPatch.Transpiler),
                        BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(lambdaMethod, transpiler: new HarmonyMethod(transpilerMethod));
            }

            Type[] compileMethodParameters = new Type[] {
                ReflectionHelper.ByName("UdonSharp.Compiler.CompilationContext"),
                typeof(IReadOnlyDictionary<,>).MakeGenericType(typeof(string), ReflectionHelper.ByName("UdonSharp.Compiler.UdonSharpCompilerV1/ProgramAssetInfo")),
                typeof(IEnumerable<string>),
                typeof(string[])
            };

            MethodInfo compileMethod = typeof(UdonSharpCompilerV1).GetMethod("Compile", BindingFlags.NonPublic | BindingFlags.Static, null, compileMethodParameters, null);
            if (compileMethod == null) {
                Type[] compileMethodParametersMerlin = new Type[] {
                    ReflectionHelper.ByName("UdonSharp.Compiler.CompilationContext"),
                    typeof(IReadOnlyDictionary<,>).MakeGenericType(typeof(string), ReflectionHelper.ByName("UdonSharp.Compiler.UdonSharpCompilerV1/ProgramAssetInfo")),
                    typeof(IEnumerable<>).MakeGenericType(ReflectionHelper.ByName("UdonSharp.Compiler.CompilationContext/ScriptAssembly"))
                };
                compileMethod = typeof(UdonSharpCompilerV1).GetMethod("Compile", BindingFlags.NonPublic | BindingFlags.Static, null, compileMethodParametersMerlin, null);
            }
            if (compileMethod != null) {
                MethodInfo transpilerMethod = typeof(CompilePatch).GetMethod(nameof(CompilePatch.Transpiler), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(compileMethod, transpiler: new HarmonyMethod(transpilerMethod));
            }

            MethodInfo getDeclarationStrMethod = ReflectionHelper.GetMethod(
                ReflectionHelper.ByName("UdonSharp.Compiler.Emit.Value"), "GetDeclarationStr", Type.EmptyTypes,
                BindingFlags.Public | BindingFlags.Instance);
            if (getDeclarationStrMethod != null) {
                MethodInfo postfixMethod =
                    typeof(GetDeclarationStrPatch).GetMethod(nameof(GetDeclarationStrPatch.Postfix),
                        BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(getDeclarationStrMethod, postfix: new HarmonyMethod(postfixMethod));
            }

            MethodInfo methodSymbolEmitMethod = ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.MethodSymbol")
                .GetMethod("Emit", BindingFlags.Public | BindingFlags.Instance);
            if (methodSymbolEmitMethod != null) {
                MethodInfo prefixMethod = typeof(MethodSymbolEmitPatch).GetMethod(nameof(MethodSymbolEmitPatch.Prefix),
                    BindingFlags.Static | BindingFlags.Public);
                MethodInfo postfixMethod =
                    typeof(MethodSymbolEmitPatch).GetMethod(nameof(MethodSymbolEmitPatch.PostFix),
                        BindingFlags.Static | BindingFlags.Public);
                MethodInfo transpileMethod = typeof(MethodSymbolEmitPatch).GetMethod(
                    nameof(MethodSymbolEmitPatch.Transpiler), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(methodSymbolEmitMethod, new HarmonyMethod(prefixMethod),
                    new HarmonyMethod(postfixMethod), new HarmonyMethod(transpileMethod));
            }

            MethodInfo emitReturnMethod = ReflectionHelper.GetMethod(
                ReflectionHelper.ByName("UdonSharp.Compiler.Emit.EmitContext"), "EmitReturn", Type.EmptyTypes,
                BindingFlags.Public | BindingFlags.Instance);
            if (emitReturnMethod != null) {
                MethodInfo prefixMethod = typeof(EmitContextEmitReturnPatch).GetMethod(
                    nameof(EmitContextEmitReturnPatch.Prefix), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(emitReturnMethod, new HarmonyMethod(prefixMethod));
            }
        }

        [MenuItem(MenuName)]
        public static void ToggleProfiler() {
            Toggle(!_enabled);
            UdonSharpCompilerV1.Compile(new UdonSharpCompileOptions { IsEditorBuild = true });
        }

        private static void Toggle(bool value) {
            _harmony ??= new Harmony("UdonSharpProfiler.DeltaNeverUsed.patch");

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