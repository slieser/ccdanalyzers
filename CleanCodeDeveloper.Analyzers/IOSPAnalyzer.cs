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
        private const string MessageFormat = "Method '{0}' mixes integration with operation. - {1}{2}.";
        private const string Description = "Integration Operation Segregation Principle (IOSP) is violated.";

        private readonly static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                "CCD0001",
                Title,
                MessageFormat,
                "Clean Code Developer Principles",
                DiagnosticSeverity.Warning,
                true,
                Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCodeBlockAction(CodeBlockAction);
        }

        private static void CodeBlockAction(CodeBlockAnalysisContext codeBlockAnalysisContext) {
            if (codeBlockAnalysisContext.OwningSymbol.Kind != SymbolKind.Method) {
                return;
            }

            var operations = new List<string>();
            var expressions = new List<string>();
            var integrations = new List<string>();
            
            var method = (IMethodSymbol) codeBlockAnalysisContext.OwningSymbol;
            var block = (BlockSyntax) codeBlockAnalysisContext.CodeBlock.ChildNodes().FirstOrDefault(n => n.IsKind(SyntaxKind.Block));
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
                    if (!integrations.Contains(methodSymbol.Name)) {
                        integrations.Add(methodSymbol.Name);
                    }
                }
                else {
                    if (methodSymbol.MethodKind is MethodKind.DelegateInvoke) {
                        continue;
                    }
                    if (!operations.Contains(methodSymbol.Name)) {
                        operations.Add(methodSymbol.Name);
                    }
                }
            }

            var expressionSymbols = FindAll(block, false, node => node is BinaryExpressionSyntax && !(node.Parent is ForStatementSyntax));
            foreach (var e in expressionSymbols) {
                if (!expressions.Contains(e.ToString())) {
                    expressions.Add(e.ToString());
                }
            }

            if (!((operations.Any() || expressions.Any()) && integrations.Any())) {
                return;
            }
                
            var tree = block.SyntaxTree;
            var location = method.Locations.First(l => tree.Equals(l.SourceTree));
            var integrationMessage = FormatIntegrations(integrations);
            var operationMessage = FormatOperations(operations, expressions);
            var diagnostic = Diagnostic.Create(Rule, location, method.Name, integrationMessage, operationMessage);
            codeBlockAnalysisContext.ReportDiagnostic(diagnostic);
        }

        private static string FormatIntegrations(List<string> integrations) {
            var result = "";
            foreach (var integration in integrations) {
                result += $"  Integration: call to '{integration}'\n";
            }
            return result;
        }

        private static string FormatOperations(List<string> operations, List<string> expressions) {
            var result = "";
            foreach (var operation in operations) {
                result += $"  Operation: calling API '{operation}'\n";
            }
            foreach (var expression in expressions) {
                result += $"  Operation: expression '{expression}'\n";
            }
            return result;
        }

        private static IEnumerable<SyntaxNode> FindAll(SyntaxNode block, bool recursive, Func<SyntaxNode, bool> predicate) {
            var result = new List<SyntaxNode>();

            var nodes = block.ChildNodes().Where(predicate).ToList();
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