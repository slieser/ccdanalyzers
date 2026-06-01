using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

internal static class TestHelpers
{
    public const string DefaultNamespacesFile = """
        # Default namespaces ignored by CCD0001 (IOSP).
        NUnit.Framework
        Moq
        VerifyNUnit
        Microsoft.Extensions.Logging
        """;

    public static CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> Build(
        string source,
        params DiagnosticResult[] expected) {
        var test = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = { Sources = { source } }
        };
        foreach (var diagnostic in expected) {
            test.ExpectedDiagnostics.Add(diagnostic);
        }
        return test;
    }

    public static CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> WithDefaultNamespaces(
        this CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> test) {
        test.TestState.AdditionalFiles.Add(("namespaces.txt", DefaultNamespacesFile));
        return test;
    }

    public static CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> WithNamespacesFile(
        this CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> test,
        string content) {
        test.TestState.AdditionalFiles.Add(("namespaces.txt", content));
        return test;
    }

    public static CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> WithEditorConfig(
        this CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> test,
        string content) {
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", content));
        return test;
    }

    public static CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> WithNet9(
        this CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> test) {
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        return test;
    }

    public static CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> WithPackages(
        this CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> test,
        params PackageIdentity[] packages) {
        test.ReferenceAssemblies = (test.ReferenceAssemblies ?? ReferenceAssemblies.Net.Net90)
            .AddPackages(ImmutableArray.CreateRange(packages));
        return test;
    }
}
