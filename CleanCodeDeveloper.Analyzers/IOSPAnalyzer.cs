using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CleanCodeDeveloper.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IOSPAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "IOSP violation";
        private const string MessageFormat = "Method '{0}' mixes integration with operation.\n{1}{2}";
        private const string Description = "Extract operational code into method and call it here";

        private static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                "CCD0001",
                Title,
                MessageFormat,
                "Clean Code Developer Principles",
                DiagnosticSeverity.Warning,
                true,
                Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCodeBlockAction(CodeBlockAction);
        }

        private static void CodeBlockAction(CodeBlockAnalysisContext codeBlockAnalysisContext) {
            if (codeBlockAnalysisContext.OwningSymbol.Kind != SymbolKind.Method) {
                return;
            }

            var isOperation = false;
            var operation = "";
            var isIntegration = false;
            var integration = "";
            
            var method = (IMethodSymbol) codeBlockAnalysisContext.OwningSymbol;
            var block = (BlockSyntax) codeBlockAnalysisContext.CodeBlock.ChildNodes().FirstOrDefault(n => n.Kind() == SyntaxKind.Block);
            if (block == null || block.Statements.Count <= 0) {
                return;
            }

            var invocations = FindAll(block, true, node => node.IsKind(SyntaxKind.InvocationExpression));
            foreach (var invocation in invocations) {
                var methodSymbol = codeBlockAnalysisContext
                    .SemanticModel
                    .GetSymbolInfo(invocation, codeBlockAnalysisContext.CancellationToken)
                    .Symbol as IMethodSymbol;
                    
                if(methodSymbol == null) continue;
                if (methodSymbol.DeclaringSyntaxReferences.Length > 0) {
                    isIntegration = true;
                    integration += $"  Integration: call to '{methodSymbol.Name}'\n";
                }
                else {
                    isOperation = true;
                    operation += $"  Operation: calling API '{methodSymbol.Name}'\n";
                }
            }

            var expressions = FindAll(block, false, node => node is BinaryExpressionSyntax);
            if (expressions.Any()) {
                isOperation = true;
                operation += string.Join("\n", expressions.Select(e => $"  Operation: expression '{e}'")) + "\n";
            }

            if (!(isOperation && isIntegration)) {
                return;
            }
                
            var tree = block.SyntaxTree;
            var location = method.Locations.First(l => tree.Equals(l.SourceTree));
            var diagnostic = Diagnostic.Create(Rule, location, method.Name, integration, operation);
            codeBlockAnalysisContext.ReportDiagnostic(diagnostic);
        }

        private static IEnumerable<SyntaxNode> FindAll(SyntaxNode block, bool recursive, Func<SyntaxNode, bool> predicate) {
            var result = new List<SyntaxNode>();

            var nodes = block.ChildNodes().Where(predicate);
            result.AddRange(nodes);
            if (nodes.Any() && !recursive) {
                return result;
            }
            foreach (var childNode in block.ChildNodes()) {
                var innerResult = FindAll(childNode, recursive, predicate);
                result.AddRange(innerResult);
            }
                    
            return result;
        }
    }
}