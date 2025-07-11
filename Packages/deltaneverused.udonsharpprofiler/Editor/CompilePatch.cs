﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UdonSharpProfiler {
    public static class CompilePatch {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator, MethodBase original) {
            List<CodeInstruction> originalCodes = instructions.ToList();
            List<CodeInstruction> codes = new(originalCodes);

            int injectIndex =
                -1; // Finds the place inside the first foreach loop of the compile function to inject the syntax walker into.
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode != OpCodes.Ldelem_Ref) continue;
                injectIndex = i + 1;
                break;
            }

            if (injectIndex == -1) {
                Injections.PrintError("Failed to find place to inject udon timer method calls");
                return originalCodes;
            }

            int cancelTokenIndex = original.GetMethodBody().LocalVariables
                .FirstOrDefault(v => v.LocalType == typeof(CancellationToken)).LocalIndex;

            // List that holds the code we're going to inject.
            List<CodeInstruction> syntaxRebuilder = new();

            // New local variables that I'm using.
            LocalBuilder binding = generator.DeclareLocal(ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding"));
            LocalBuilder tree = generator.DeclareLocal(typeof(SyntaxNode));
            LocalBuilder walker = generator.DeclareLocal(typeof(UdonSharpTimerInjector));
            LocalBuilder newroot = generator.DeclareLocal(typeof(UdonSharpTimerInjector));

            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stloc_S, binding)); // store binding


            // Getting the root node of the syntax tree from UdonSharp's ModuleBinding
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldfld,
                ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding").GetField("tree")));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloca_S, cancelTokenIndex));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Initobj, typeof(CancellationToken)));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, cancelTokenIndex));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Callvirt,
                typeof(SyntaxTree).GetMethod("GetRoot", new[] { typeof(CancellationToken) })));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stloc_S, tree));

            // Creating the syntax walker object
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Newobj,
                ReflectionHelper.GetConstructor(typeof(UdonSharpTimerInjector), new Type[] { },
                    BindingFlags.Public | BindingFlags.Instance)));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stloc_S, walker));

            // Running the walker on the syntax tree
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, walker));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, tree));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Callvirt,
                typeof(CSharpSyntaxVisitor<>).MakeGenericType(typeof(SyntaxNode))
                    .GetMethod("Visit", new[] { typeof(SyntaxNode) })));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stloc_S, newroot));

            // Create new syntax tree from new root
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, newroot));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldfld,
                ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding").GetField("tree")));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Callvirt,
                typeof(SyntaxTree).GetProperty("Options").GetGetMethod()));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldfld,
                ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding").GetField("tree")));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Callvirt,
                typeof(SyntaxTree).GetProperty("FilePath").GetGetMethod()));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldnull));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Call, typeof(SyntaxFactory).GetMethod("SyntaxTree", new[] {
                typeof(SyntaxNode),
                typeof(ParseOptions),
                typeof(string),
                typeof(Encoding)
            })));


            // Setting old root to new root
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Stfld,
                ReflectionHelper.ByName("UdonSharp.Compiler.ModuleBinding").GetField("tree")));
            syntaxRebuilder.Add(new CodeInstruction(OpCodes.Ldloc_S, binding));


            // Inject IL into the compile method
            codes.InsertRange(injectIndex,
                syntaxRebuilder);

            return codes.AsEnumerable();
        }
    }
}