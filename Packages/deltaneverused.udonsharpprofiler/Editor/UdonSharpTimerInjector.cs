using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;

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
            if (__getProfilerDataReader) {
                __profilerDataReader = UdonSharpProfiler.UdonProfilerConsts.GetComponentNonProfiled<UdonSharpProfiler.ProfilerDataReader>(UnityEngine.GameObject.Find(""Profiler""));
                __getProfilerDataReader = !global::VRC.SDKBase.Utilities.IsValid(__profilerDataReader);
                if (__getProfilerDataReader) {
                    UnityEngine.Debug.LogError(""Couldn't find profiler in scene, make sure it's name is \""Profiler\"""");
                    return;
                }   
            }
            
            global::VRC.SDK3.Data.DataDictionary info = new global::VRC.SDK3.Data.DataDictionary();
            string functionName = (string)GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchNameKey);

            info.Add(""name"", functionName);
            info.Add(""start"", System.Diagnostics.Stopwatch.GetTimestamp());

            __profilerDataReader.QueuePacket(info);
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
            if (__getProfilerDataReader) {
                __profilerDataReader = UdonSharpProfiler.UdonProfilerConsts.GetComponentNonProfiled<UdonSharpProfiler.ProfilerDataReader>(UnityEngine.GameObject.Find(""Profiler""));
                __getProfilerDataReader = !global::VRC.SDKBase.Utilities.IsValid(__profilerDataReader);
                if (__getProfilerDataReader) {
                    UnityEngine.Debug.LogError(""Couldn't find profiler in scene, make sure it's name is \""Profiler\"""");
                    return;
                }
            }
            
            global::VRC.SDK3.Data.DataDictionary info = new global::VRC.SDK3.Data.DataDictionary();
            info.Add(""end"", System.Diagnostics.Stopwatch.GetTimestamp());

            __profilerDataReader.QueuePacket(info);
}")
            ));

        public static FieldDeclarationSyntax CreateField<T>(string name, string defaultValue, string accessibility = "private") {
            string code = $"{accessibility} {typeof(T).FullName} {name} = {defaultValue};";
            return (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(code);
        }

        public override SyntaxNode Visit(SyntaxNode node) {
            if (!_isRoot)
                return base.Visit(node);
            
            _isRoot = false;
            _root = node as CompilationUnitSyntax;

            if (_root != null) {
                UsingDirectiveSyntax dataDictAlias = SyntaxFactory.UsingDirective(
                        SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Profiler_Data")),
                        SyntaxFactory.ParseName("global::VRC.SDK3.Data"))
                    .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

                UsingDirectiveSyntax utilitiesAlias = SyntaxFactory.UsingDirective(
                        SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Profiler_Utilities")),
                        SyntaxFactory.ParseName("global::VRC.SDKBase.Utilities"))
                    .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

                _root = _root.AddUsings(dataDictAlias, utilitiesAlias);

                // Inject timing functions into the base classes
                IEnumerable<ClassDeclarationSyntax> classDeclarations =
                    _root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (ClassDeclarationSyntax classDeclaration in classDeclarations) {
                    bool isStatic =
                        classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
                    bool inheritsFromBaseClass = classDeclaration.BaseList?.Types
                        .Any(baseType => baseType.ToString() == "UdonSharpBehaviour") ?? false;
                    if (isStatic || !inheritsFromBaseClass)
                        continue;

                    ClassDeclarationSyntax newClassDeclaration = classDeclaration.AddMembers(_startTimingMethod, _endTimingMethod, CreateField<ProfilerDataReader>("__profilerDataReader", "null"), CreateField<bool>("__getProfilerDataReader", "true"));
                    node = _root.ReplaceNode(classDeclaration, newClassDeclaration);
                    //Debug.Log(node.ToFullString());
                }
            }
            else {
                Injections.PrintError("Root was null?");
            }

            return base.Visit(node);
        }
    }
}
