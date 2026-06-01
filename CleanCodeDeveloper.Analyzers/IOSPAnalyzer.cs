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
        private const string ExcludeTestsOptionKey = "dotnet_diagnostic.CCD0001.exclude_tests";
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

        // Attributes (by full name) that mark a method as a test or a test lifecycle method in
        // NUnit, MSTest and xUnit. Matched per method, never per containing type.
        private static readonly ImmutableHashSet<string> TestAttributeNames = ImmutableHashSet.Create(
            StringComparer.Ordinal,
            // NUnit
            "NUnit.Framework.TestAttribute",
            "NUnit.Framework.TestCaseAttribute",
            "NUnit.Framework.TestCaseSourceAttribute",
            "NUnit.Framework.TheoryAttribute",
            "NUnit.Framework.SetUpAttribute",
            "NUnit.Framework.TearDownAttribute",
            "NUnit.Framework.OneTimeSetUpAttribute",
            "NUnit.Framework.OneTimeTearDownAttribute",
            // MSTest
            "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute",
            "Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute",
            "Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute",
            "Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute",
            "Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute",
            "Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute",
            // xUnit
            "Xunit.FactAttribute",
            "Xunit.TheoryAttribute");

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
            var method = ResolveOwningMethod(context.OwningSymbol);
            if (method == null) {
                return;
            }
            if (GetExcludeTests(context) && IsTestMethod(method)) {
                return;
            }
            if (IsInIgnoredNamespace(method.ContainingNamespace, namespacesToIgnore)) {
                return;
            }
            var body = GetMethodBody(context.CodeBlock);
            if (body == null) {
                return;
            }

            var integrationKeys = new HashSet<string>(StringComparer.Ordinal);
            var integrations = new SortedSet<string>(StringComparer.Ordinal);
            var operationKeys = new HashSet<string>(StringComparer.Ordinal);
            var operations = new SortedSet<string>(StringComparer.Ordinal);
            var expressions = new SortedSet<string>(StringComparer.Ordinal);

            ClassifyInvocations(context, method, body, namespacesToIgnore, integrationKeys, integrations, operationKeys, operations);
            CollectExpressions(body, expressions);

            if (!((operationKeys.Count > 0 || expressions.Count > 0) && integrationKeys.Count > 0)) {
                return;
            }

            var metric = CalculateMetric(operationKeys.Count, expressions.Count, integrationKeys.Count);
            if (metric < GetMinMetric(context)) {
                return;
            }

            var syntaxTree = body.SyntaxTree;
            var location = method.Locations.FirstOrDefault(l => syntaxTree.Equals(l.SourceTree));
            if (location == null) {
                return;
            }

            var integrationMessage = FormatIntegrations(integrations);
            var operationMessage = FormatOperations(operations, expressions);
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.Name, metric, integrationMessage, operationMessage));
        }

        private static IMethodSymbol? ResolveOwningMethod(ISymbol owningSymbol) =>
            owningSymbol switch {
                IMethodSymbol method => method,
                IPropertySymbol property => property.GetMethod ?? property.SetMethod,
                _ => null
            };

        private static SyntaxNode? GetMethodBody(SyntaxNode codeBlock) {
            foreach (var child in codeBlock.ChildNodes()) {
                if (child is BlockSyntax block) {
                    return block.Statements.Count > 0 ? block : null;
                }
                if (child is ArrowExpressionClauseSyntax arrow) {
                    return arrow.Expression;
                }
            }
            return null;
        }

        private static void ClassifyInvocations(
            CodeBlockAnalysisContext context,
            IMethodSymbol owningMethod,
            SyntaxNode body,
            ImmutableArray<string> namespacesToIgnore,
            HashSet<string> integrationKeys,
            SortedSet<string> integrations,
            HashSet<string> operationKeys,
            SortedSet<string> operations) {

            foreach (var invocation in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()) {
                if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol) {
                    continue;
                }
                if (IsInIgnoredNamespace(methodSymbol.ContainingNamespace, namespacesToIgnore)) {
                    continue;
                }
                if (IsInsideIgnoredCallSpecification(invocation, context, namespacesToIgnore)) {
                    continue;
                }
                if (methodSymbol.IsVirtual && methodSymbol.Name == owningMethod.Name) {
                    // base.X() call inside an override is neither integration nor operation
                    continue;
                }

                if (methodSymbol.DeclaringSyntaxReferences.Length > 0) {
                    if (integrationKeys.Add(MethodKey(methodSymbol))) {
                        integrations.Add(methodSymbol.Name);
                    }
                    continue;
                }

                if (IsDelegateInvoke(methodSymbol)) {
                    if (integrationKeys.Add(MethodKey(methodSymbol))) {
                        integrations.Add(methodSymbol.Name);
                    }
                    continue;
                }
                if (IsTaskRun(methodSymbol) || IsConfigureAwait(methodSymbol)) {
                    continue;
                }
                if (operationKeys.Add(MethodKey(methodSymbol))) {
                    operations.Add(methodSymbol.Name);
                }
            }
        }

        // A call that appears inside a lambda argument of an ignored-namespace call (e.g. the
        // expression in Moq's mock.Setup(x => x.Foo())) describes that call rather than executing
        // it, so it must not be classified once the framework's namespace is ignored.
        private static bool IsInsideIgnoredCallSpecification(
            InvocationExpressionSyntax invocation,
            CodeBlockAnalysisContext context,
            ImmutableArray<string> namespacesToIgnore) {
            for (var node = invocation.Parent; node != null; node = node.Parent) {
                if (IsFunctionDeclaration(node)) {
                    return false;
                }
                if (node is AnonymousFunctionExpressionSyntax lambda
                    && EnclosingInvocationIsIgnored(lambda, context, namespacesToIgnore)) {
                    return true;
                }
            }
            return false;
        }

        private static bool EnclosingInvocationIsIgnored(
            AnonymousFunctionExpressionSyntax lambda,
            CodeBlockAnalysisContext context,
            ImmutableArray<string> namespacesToIgnore) {
            for (var node = lambda.Parent; node != null; node = node.Parent) {
                if (IsFunctionDeclaration(node)) {
                    return false;
                }
                if (node is InvocationExpressionSyntax invocation) {
                    return context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol method
                           && IsInIgnoredNamespace(method.ContainingNamespace, namespacesToIgnore);
                }
            }
            return false;
        }

        private static bool IsFunctionDeclaration(SyntaxNode node) =>
            node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax;

        private static string MethodKey(IMethodSymbol m) {
            var typeName = m.ContainingType?.ToDisplayString() ?? "?";
            return typeName + "." + m.Name;
        }

        private static void CollectExpressions(SyntaxNode body, SortedSet<string> expressions) {
            foreach (var expression in body.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>()) {
                if (IsCanonicalForLoopCondition(expression)) {
                    continue;
                }
                if (HasBinaryExpressionAncestorWithin(expression, body)) {
                    continue;
                }
                expressions.Add(expression.ToString());
            }
        }

        private static bool IsCanonicalForLoopCondition(BinaryExpressionSyntax expression) =>
            expression.Parent is ForStatementSyntax forStatement && forStatement.Condition == expression;

        private static bool HasBinaryExpressionAncestorWithin(SyntaxNode node, SyntaxNode boundary) {
            if (node == boundary) {
                return false;
            }
            for (var parent = node.Parent; parent != null; parent = parent.Parent) {
                if (parent is BinaryExpressionSyntax) {
                    return true;
                }
                if (parent == boundary) {
                    return false;
                }
            }
            return false;
        }

        private static bool IsInIgnoredNamespace(INamespaceSymbol? containingNamespace, ImmutableArray<string> namespacesToIgnore) {
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
            var containingType = methodSymbol.ContainingType?.ToDisplayString() ?? string.Empty;
            return containingType.StartsWith(ActionTypePrefix, StringComparison.Ordinal)
                   || containingType.StartsWith(FuncTypePrefix, StringComparison.Ordinal);
        }

        private static bool IsTaskRun(IMethodSymbol methodSymbol) =>
            string.Equals(methodSymbol.Name, "Run", StringComparison.Ordinal)
            && string.Equals(methodSymbol.ContainingNamespace?.Name, TasksNamespace, StringComparison.Ordinal);

        private static bool IsConfigureAwait(IMethodSymbol methodSymbol) =>
            string.Equals(methodSymbol.Name, "ConfigureAwait", StringComparison.Ordinal)
            && string.Equals(methodSymbol.ContainingNamespace?.Name, TasksNamespace, StringComparison.Ordinal);

        private static bool GetExcludeTests(CodeBlockAnalysisContext context) {
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.SemanticModel.SyntaxTree);
            return options.TryGetValue(ExcludeTestsOptionKey, out var value)
                   && bool.TryParse(value, out var excludeTests)
                   && excludeTests;
        }

        private static bool IsTestMethod(IMethodSymbol method) {
            foreach (var attribute in method.GetAttributes()) {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                if (attributeName != null && TestAttributeNames.Contains(attributeName)) {
                    return true;
                }
            }
            return false;
        }

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
