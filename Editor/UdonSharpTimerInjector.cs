﻿using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UdonSharpProfiler {
    public class UdonSharpTimerInjector : CSharpSyntaxRewriter {
        private bool _isRoot = true;
        private CompilationUnitSyntax _root;

        private MethodDeclarationSyntax _startTimingMethod => SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                SyntaxFactory.Identifier("Profiler_StartTiming"))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAttributeLists(SyntaxFactory.SingletonList(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("UdonSharpProfiler"),
                        SyntaxFactory.IdentifierName("DontUdonProfile")))))))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(
                    @"{
var fakeSelf = (UdonSharp.UdonSharpBehaviour)GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchSelfKey);
var root = (VRC.SDK3.Data.DataDictionary)fakeSelf.GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchHeapKey);
var parent = (VRC.SDK3.Data.DataDictionary)fakeSelf.GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchHeapParentKey);

var info = new VRC.SDK3.Data.DataDictionary();

var name = (string)GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchNameKey);

info.Add(""parent"", parent);
info.Add(""name"", name);
info.Add(""start"", System.Diagnostics.Stopwatch.GetTimestamp());
info.Add(""end"", (long)0);
info.Add(""children"", new VRC.SDK3.Data.DataList());

if (VRC.SDKBase.Utilities.IsValid(parent)){
    parent[""children""].DataList.Add(info);
}
else {
    root[name] = info;
}

fakeSelf.SetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchHeapKey, root);
fakeSelf.SetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchHeapParentKey, info);
}")
            ));

        private MethodDeclarationSyntax _endTimingMethod => SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                SyntaxFactory.Identifier("Profiler_EndTiming"))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithAttributeLists(SyntaxFactory.SingletonList(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("UdonSharpProfiler"),
                        SyntaxFactory.IdentifierName("DontUdonProfile")))))))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(
                    @"{
var fakeSelf = (UdonSharp.UdonSharpBehaviour)GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchSelfKey);
var parent = (VRC.SDK3.Data.DataDictionary)fakeSelf.GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchHeapParentKey);
if (VRC.SDKBase.Utilities.IsValid(parent)) {
    parent[""end""] = System.Diagnostics.Stopwatch.GetTimestamp();
    if (parent.TryGetValue(""parent"", VRC.SDK3.Data.TokenType.DataDictionary, out var value)) {
        fakeSelf.SetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchHeapParentKey, value.DataDictionary);
    } else {
        fakeSelf.SetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchHeapParentKey, null);
    }
}}")
            ));

        public override SyntaxNode Visit(SyntaxNode node) {
            if (_isRoot) {
                _isRoot = false;
                _root = node as CompilationUnitSyntax;

                if (_root != null) {
                    // Inject timing functions into the base classes
                    var classDeclarations = _root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                    foreach (var classDeclaration in classDeclarations) {
                        var newClassDeclaration = classDeclaration.AddMembers(_startTimingMethod, _endTimingMethod);
                        var isStatic =
                            classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
                        var inheritsFromBaseClass = classDeclaration.BaseList?.Types
                            .Any(baseType => baseType.ToString() == "UdonSharpBehaviour") ?? false;
                        if (!isStatic &&
                            inheritsFromBaseClass) // Don't if it's not a UdonSharpBehaviour or the class is static
                            node = _root.ReplaceNode(classDeclaration, newClassDeclaration);
                    }
                }
                else {
                    Injections.PrintError("Root was null?");
                }
            }

            return base.Visit(node);
        }
    }
}