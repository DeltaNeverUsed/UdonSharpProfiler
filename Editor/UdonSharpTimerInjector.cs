using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;

namespace UdonSharpProfiler {
    public class UdonSharpTimerInjector : CSharpSyntaxRewriter {
        //private readonly string _namespace;

        private ClassDeclarationSyntax _currentClass;

        private bool _isRoot = true;

        public UdonSharpTimerInjector() {
            //_namespace = "UdonSharpProfiler";
            /*           @"
           using System;
           using System.Diagnostics;
           using UdonSharp;
           using UnityEngine;
           using VRC.SDK3.Data;
           public class TestProfilerClass : UdonSharpBehaviour {
               [DontUdonProfile]
               public void Profiler_StartTiming(string funcName) {
                   //Debug.Log($""Profiler_StartTiming: caller: {funcName}"");

                   var root = (DataDictionary)GetProgramVariable(UdonProfilerConsts.StopwatchHeapKey);
                   var parent = (DataDictionary)GetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey);

                   var info = new DataDictionary();

                   info.Add(""parent"", parent);
                   info.Add(""name"", funcName);
                   info.Add(""start"", Stopwatch.GetTimestamp());
                   info.Add(""end"", -1);
                   //info.Add(""timer"", new DataToken(stopwatch));
                   info.Add(""children"", new DataList());

                   if (parent == null) {
                       root[funcName] = info;
                   }
                   else {
                       parent[""children""].DataList.Add(info);
                   }

                   SetProgramVariable(UdonProfilerConsts.StopwatchHeapKey, root);
                   SetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey, info);
               }
           }
           ");

                       _stopTiming = ParseMethod(@"
           using System;
           using System.Diagnostics;
           using UdonSharp;
           using UnityEngine;
           using VRC.SDK3.Data;
           public class TestProfilerClass : UdonSharpBehaviour {
               [DontUdonProfile]
               public void Profiler_EndTiming() {
                   var parent = (DataDictionary)GetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey);
                   if (parent != null) {
                       parent[""end""] = Stopwatch.GetTimestamp();
                       //((Stopwatch)parent[""timer""].Reference).Stop();
                       if (parent.TryGetValue(""parent"", TokenType.DataDictionary, out var value))
                           SetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey, value.DataDictionary);
                       else
                           SetProgramVariable(UdonProfilerConsts.StopwatchHeapParentKey, null);
                   }
               }
           }");*/
        }

        public static MethodDeclarationSyntax ParseMethod(string methodCode) {
            var methodSyntaxTree = CSharpSyntaxTree.ParseText(methodCode);
            var root = methodSyntaxTree.GetRoot() as CompilationUnitSyntax;

            return root.Members[0].ChildNodes().ToArray()[1] as MethodDeclarationSyntax;
        }

        public override SyntaxNode Visit(SyntaxNode node) {
            if (_isRoot) {
                _isRoot = false;
                var root = node as CompilationUnitSyntax;

                var startTimingMethod = SyntaxFactory.MethodDeclaration(
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
                var endTimingMethod = SyntaxFactory.MethodDeclaration(
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

                var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDeclaration != null) {
                    var newClassDeclaration = classDeclaration.AddMembers(startTimingMethod, endTimingMethod);
                    bool isStatic = classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
                    bool inheritsFromBaseClass = classDeclaration.BaseList?.Types
                        .Any(baseType => baseType.ToString() == "UdonSharpBehaviour") ?? false;
                    if (!isStatic && inheritsFromBaseClass)
                        node = root.ReplaceNode(classDeclaration, newClassDeclaration);
                }
            }

            {
                if (node is ClassDeclarationSyntax classDeclaration) {
                    _currentClass = classDeclaration;
                }
            }

            return base.Visit(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
            if (node.Body == null) return node;

            if (node.Modifiers.Any(SyntaxKind.StaticKeyword)) // Ignore static functions for now
                return node;

            if (_currentClass?.BaseList == null || !_currentClass.BaseList.Types.Any(b =>
                    b.Type is IdentifierNameSyntax { Identifier: { Text: "UdonSharpBehaviour" } }))
                return node;

            var dontProfile = node.AttributeLists
                .SelectMany(list => list.Attributes)
                .Any(attr => attr.Name.ToString().Contains("DontUdonProfile", StringComparison.OrdinalIgnoreCase));

            if (dontProfile) {
                Debug.Log("Profiling skipped on: " + node.ToFullString());
                return node;
            }

            /*
            var startTimingStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParseTypeName("UdonSharpProfiler.UdonStaticFunctions"),
                        SyntaxFactory.IdentifierName("Profiler_StartTiming")
                    ),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] {
                        SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(node.Identifier.ToString())))
                    }))
                ));

            var endTimingStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParseTypeName("UdonSharpProfiler.UdonStaticFunctions"),
                        SyntaxFactory.IdentifierName("Profiler_EndTiming")
                    ),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                        { SyntaxFactory.Argument(SyntaxFactory.ThisExpression()) }))
                ));
                */

            return node;

            var startTimingStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("Profiler_StartTiming") /*,
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] {
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(node.Identifier.ToString())))
                    }))*/
                ));

            var endTimingStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("Profiler_EndTiming")
                    //SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                    //    { SyntaxFactory.Argument(SyntaxFactory.ThisExpression()) }))
                ));


            var newBody = node.Body.WithStatements(
                node.Body.Statements.Insert(0, startTimingStatement) //.Add(endTimingStatement)
            );


            //Debug.Log(node.WithBody(newBody).ToFullString());
            return node.WithBody(newBody);
        }
    }
}