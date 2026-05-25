using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
        private const string MessageFormat = "Method '{0}' mixes integration with operation. Metric = {1}\n{2}{3}";
        private const string Description = "Integration Operation Segregation Principle (IOSP) is violated.";
        private const string MinMetricOptionKey = "dotnet_diagnostic.CCD0001.min_metric";
        private const string AdditionalNamespacesOptionKey = "dotnet_diagnostic.CCD0001.additional_namespaces";
        private const string NamespacesFileName = "namespaces.txt";
        private const int DefaultMinMetric = 1;
        private const string TasksNamespace = "Tasks";
        private const string ActionTypePrefix = "System.Action";
        private const string FuncTypePrefix = "System.Func";

        private static readonly DiagnosticDescriptor Rule =
            new("CCD0001",
                Title,
                MessageFormat,
                "Clean Code Developer Principles",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: Description,
                helpLinkUri: "https://ccd-akademie.de/iosp-analyzer");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(startContext => {
                var namespacesToIgnore = GetNamespacesToIgnore(startContext);
                startContext.RegisterCodeBlockAction(codeBlockContext =>
                    CodeBlockAction(codeBlockContext, namespacesToIgnore));
            });
        }

        private static ImmutableArray<string> GetNamespacesToIgnore(CompilationStartAnalysisContext context) {
            var fromFile = ReadNamespacesFile(context);
            var fromConfig = ReadAdditionalNamespacesFromConfig(context);
            return fromFile.AddRange(fromConfig);
        }

        private static ImmutableArray<string> ReadNamespacesFile(CompilationStartAnalysisContext context) {
            var namespaceFile = context.Options.AdditionalFiles
                .FirstOrDefault(file => string.Equals(Path.GetFileName(file.Path), NamespacesFileName, StringComparison.OrdinalIgnoreCase));
            if (namespaceFile == null) {
                return [];
            }
            var fileText = namespaceFile.GetText(context.CancellationToken);
            if (fileText == null) {
                return [];
            }
            return fileText.Lines
                .Select(line => line.ToString().Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                .ToImmutableArray();
        }

        private static ImmutableArray<string> ReadAdditionalNamespacesFromConfig(CompilationStartAnalysisContext context) {
            var globalOptions = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            if (!globalOptions.TryGetValue(AdditionalNamespacesOptionKey, out var configValue) || string.IsNullOrWhiteSpace(configValue)) {
                return [];
            }
            return configValue
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToImmutableArray();
        }

        private static void CodeBlockAction(CodeBlockAnalysisContext context, ImmutableArray<string> namespacesToIgnore) {
            if (context.OwningSymbol is not IMethodSymbol method) {
                return;
            }
            if (IsInIgnoredNamespace(method.ContainingNamespace, namespacesToIgnore)) {
                return;
            }
            var block = context.CodeBlock.ChildNodes().FirstOrDefault(n => n.IsKind(SyntaxKind.Block)) as BlockSyntax;
            if (block == null || block.Statements.Count == 0) {
                return;
            }

            var integrations = new SortedSet<string>(StringComparer.Ordinal);
            var operations = new SortedSet<string>(StringComparer.Ordinal);
            var expressions = new SortedSet<string>(StringComparer.Ordinal);

            ClassifyInvocations(context, method, block, namespacesToIgnore, integrations, operations);
            CollectExpressions(block, expressions);

            if (!((operations.Count > 0 || expressions.Count > 0) && integrations.Count > 0)) {
                return;
            }

            var metric = CalculateMetric(operations.Count, expressions.Count, integrations.Count);
            if (metric < GetMinMetric(context)) {
                return;
            }

            var location = method.Locations.FirstOrDefault(l => block.SyntaxTree.Equals(l.SourceTree));
            if (location == null) {
                return;
            }

            var integrationMessage = FormatIntegrations(integrations);
            var operationMessage = FormatOperations(operations, expressions);
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.Name, metric, integrationMessage, operationMessage));
        }

        private static void ClassifyInvocations(
            CodeBlockAnalysisContext context,
            IMethodSymbol owningMethod,
            BlockSyntax block,
            ImmutableArray<string> namespacesToIgnore,
            SortedSet<string> integrations,
            SortedSet<string> operations) {

            foreach (var invocation in block.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
                if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol) {
                    continue;
                }
                var containingNamespace = methodSymbol.ContainingNamespace;
                if (IsInIgnoredNamespace(containingNamespace, namespacesToIgnore)) {
                    continue;
                }
                if (methodSymbol.IsVirtual && methodSymbol.Name == owningMethod.Name) {
                    // base.X() call inside an override is neither integration nor operation
                    continue;
                }

                if (methodSymbol.DeclaringSyntaxReferences.Length > 0) {
                    integrations.Add(methodSymbol.Name);
                    continue;
                }

                if (IsDelegateInvoke(methodSymbol)) {
                    integrations.Add(methodSymbol.Name);
                    continue;
                }
                if (IsTaskRun(methodSymbol) || IsConfigureAwait(methodSymbol)) {
                    continue;
                }
                operations.Add(methodSymbol.Name);
            }
        }

        private static void CollectExpressions(BlockSyntax block, SortedSet<string> expressions) {
            // TODO: verify that in for-loops only canonical expressions (0, i < 10, i++) are used
            foreach (var expression in block.DescendantNodes().OfType<BinaryExpressionSyntax>()) {
                if (expression.Parent is ForStatementSyntax) {
                    continue;
                }
                if (HasBinaryExpressionAncestorWithin(expression, block)) {
                    continue;
                }
                expressions.Add(expression.ToString());
            }
        }

        private static bool HasBinaryExpressionAncestorWithin(SyntaxNode node, SyntaxNode boundary) {
            for (var parent = node.Parent; parent != null && parent != boundary; parent = parent.Parent) {
                if (parent is BinaryExpressionSyntax) {
                    return true;
                }
            }
            return false;
        }

        private static bool IsInIgnoredNamespace(INamespaceSymbol containingNamespace, ImmutableArray<string> namespacesToIgnore) {
            if (containingNamespace == null) {
                return false;
            }
            var display = containingNamespace.ToDisplayString();
            foreach (var prefix in namespacesToIgnore) {
                if (display.StartsWith(prefix, StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }

        private static bool IsDelegateInvoke(IMethodSymbol methodSymbol) {
            if (methodSymbol.MethodKind != MethodKind.DelegateInvoke) {
                return false;
            }
            var containingType = methodSymbol.ContainingType.ToDisplayString();
            return containingType.StartsWith(ActionTypePrefix, StringComparison.Ordinal)
                   || containingType.StartsWith(FuncTypePrefix, StringComparison.Ordinal);
        }

        private static bool IsTaskRun(IMethodSymbol methodSymbol) =>
            string.Equals(methodSymbol.Name, "Run", StringComparison.Ordinal)
            && string.Equals(methodSymbol.ContainingNamespace.Name, TasksNamespace, StringComparison.Ordinal);

        private static bool IsConfigureAwait(IMethodSymbol methodSymbol) =>
            string.Equals(methodSymbol.Name, "ConfigureAwait", StringComparison.Ordinal)
            && string.Equals(methodSymbol.ContainingNamespace.Name, TasksNamespace, StringComparison.Ordinal);

        private static int GetMinMetric(CodeBlockAnalysisContext context) {
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.SemanticModel.SyntaxTree);
            if (!options.TryGetValue(MinMetricOptionKey, out var value)) {
                return DefaultMinMetric;
            }
            if (!int.TryParse(value, out var minMetric)) {
                return DefaultMinMetric;
            }
            return minMetric < DefaultMinMetric ? DefaultMinMetric : minMetric;
        }

        private static int CalculateMetric(int operationsCount, int expressionsCount, int integrationsCount) {
            if (integrationsCount == 0) {
                return 0;
            }
            if (operationsCount == 0 && expressionsCount == 0) {
                return 0;
            }
            if (integrationsCount > operationsCount) {
                return operationsCount + 2 * expressionsCount;
            }
            return integrationsCount;
        }

        private static string FormatIntegrations(IEnumerable<string> integrations) =>
            string.Concat(integrations.Select(i => $"- Integration: call to '{i}'\n"));

        private static string FormatOperations(IEnumerable<string> operations, IEnumerable<string> expressions) =>
            string.Concat(operations.Select(o => $"- Operation: calling API '{o}'\n"))
            + string.Concat(expressions.Select(e => $"- Operation: expression '{e}'\n"));
    }
}
