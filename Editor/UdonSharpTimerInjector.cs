using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;

namespace UdonSharpProfiler {
    public class UdonSharpTimerInjector : CSharpSyntaxRewriter {
        //private readonly string _namespace;

        private ClassDeclarationSyntax _currentClass;

        public UdonSharpTimerInjector() {
            //_namespace = "UdonSharpProfiler";
        }

        public override SyntaxNode Visit(SyntaxNode node) {
            if (node is ClassDeclarationSyntax classDeclaration) {
                _currentClass = classDeclaration;
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
                .Any(attr => attr.Name.ToString() == "DontUdonProfile");

            if (dontProfile) {
                Debug.Log("Profiling skipped on: " + node.ToFullString());
                return node;
            }

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

            var newBody = node.Body.WithStatements(
                node.Body.Statements.Insert(0, startTimingStatement).Add(endTimingStatement)
            );

            //Debug.Log(node.WithBody(newBody).ToFullString());
            return node.WithBody(newBody);
        }
    }
}