using System.Linq;
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
var list = (Profiler_Data.DataList)fakeSelf.GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchListKey);
var parent = (Profiler_Data.DataDictionary)fakeSelf.GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchParentKey);

var info = new Profiler_Data.DataDictionary();
var name = (string)GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchNameKey);

info.Add(""parent"", parent);
info.Add(""name"", name);
info.Add(""start"", System.Diagnostics.Stopwatch.GetTimestamp());
info.Add(""end"", (long)0);
info.Add(""type"", (int)UdonSharpProfiler.ProfilerEventType.FunctionCall);

list.Add(info);

fakeSelf.SetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchListKey, list);
fakeSelf.SetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchParentKey, info);
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
var parent = (Profiler_Data.DataDictionary)fakeSelf.GetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchParentKey);
if (Profiler_Utilities.IsValid(parent)) {
    parent[""end""] = System.Diagnostics.Stopwatch.GetTimestamp();
    if (parent.TryGetValue(""parent"", Profiler_Data.TokenType.DataDictionary, out var value)) {
        fakeSelf.SetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchParentKey, value.DataDictionary);
    } else {
        fakeSelf.SetProgramVariable(UdonSharpProfiler.UdonProfilerConsts.StopwatchParentKey, null);
    }
}}")
            ));

        public override SyntaxNode Visit(SyntaxNode node) {
            if (_isRoot) {
                _isRoot = false;
                _root = node as CompilationUnitSyntax;

                if (_root != null) {
                    UsingDirectiveSyntax dataDictAlias = SyntaxFactory.UsingDirective(
                            SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Profiler_Data")),
                            SyntaxFactory.ParseName("VRC.SDK3.Data"))
                        .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

                    UsingDirectiveSyntax utilitiesAlias = SyntaxFactory.UsingDirective(
                            SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Profiler_Utilities")),
                            SyntaxFactory.ParseName("VRC.SDKBase.Utilities"))
                        .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

                    _root = _root.AddUsings(dataDictAlias, utilitiesAlias);
                    
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