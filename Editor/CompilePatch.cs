using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityEngine;

namespace UdonSharpProfiler {
    public static class CompilePatch {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original) {
            var originalCodes = instructions.ToList();
            var codes = new List<CodeInstruction>(originalCodes);
            
            var injectIndex = -1;
            for (var i = 0; i < codes.Count; i++) {
                if (codes[i].opcode != OpCodes.Ldelem_Ref) continue;
                //if (codes[i].operand.ToString() != "Microsoft.CodeAnalysis.SyntaxTree tree") continue;

                injectIndex = i+1;
                break;
            }

            if (injectIndex == -1) {
                Injections.PrintError("Failed to find place to inject udon timer method calls");
                return originalCodes;
            }

            /*for (int i = injectIndex-8; i < injectIndex+4; i++) {
                Debug.Log($"{codes[i].opcode.ToString()}: {codes[i].operand}");
            }*/

            var cancelTokenIndex = original.GetMethodBody().LocalVariables.FirstOrDefault(v => v.LocalType == typeof(CancellationToken)).LocalIndex;
            

            var syntaxRebuilder = new List<CodeInstruction>();

            var binding = generator.DeclareLocal(ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding"));
            var tree = generator.DeclareLocal(typeof(SyntaxNode));
            var walker = generator.DeclareLocal(typeof(UdonSharpTimerInjector));
            var newroot = generator.DeclareLocal(typeof(UdonSharpTimerInjector));
            
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stloc_S, binding)); // store binding
            
            
            // Get root node
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldfld, ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding").GetField("tree")));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloca_S, cancelTokenIndex));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Initobj, typeof(CancellationToken)));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, cancelTokenIndex));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Callvirt, typeof(SyntaxTree).GetMethod("GetRoot", new[] { typeof(CancellationToken) })));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stloc_S, tree));

            // Create syntax walker
            //syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldstr, "UdonSharpProfiler.UdonStaticFunctions.TestFunction"));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Newobj,
                ReflectionHelper.GetConstructor(typeof(UdonSharpTimerInjector), new Type[] { }, BindingFlags.Public | BindingFlags.Instance)));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stloc_S, walker));

            // Create new root
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, walker));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, tree));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Callvirt, typeof(CSharpSyntaxVisitor<>).MakeGenericType(typeof(SyntaxNode)).GetMethod("Visit", new[] { typeof(SyntaxNode) })));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stloc_S, newroot));

            // Create new syntax tree from new root
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, newroot));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldfld, ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding").GetField("tree")));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Callvirt, typeof(SyntaxTree).GetProperty("Options").GetGetMethod()));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldfld, ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding").GetField("tree")));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Callvirt, typeof(SyntaxTree).GetProperty("FilePath").GetGetMethod()));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldnull));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Call, typeof(SyntaxFactory).GetMethod("SyntaxTree", new[]
            {
                typeof(SyntaxNode),
                typeof(ParseOptions),
                typeof(string),
                typeof(System.Text.Encoding)
            })));


            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stfld, ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding").GetField("tree")));
            
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            codes.InsertRange(injectIndex,
                syntaxRebuilder);
            
            return codes.AsEnumerable();
        }
    }
}