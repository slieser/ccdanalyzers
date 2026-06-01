using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<CleanCodeDeveloper.Analyzers.IOSPAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class ExcludeTestsTests
{
    // A test method and a plain helper in the same fixture that both violate IOSP. The decision to
    // skip must be made per method, so only the [Test] method is excluded when the option is on.
    private const string FixtureWithTestAndHelper = """
        using System;
        using NUnit.Framework;

        public class Fixture
        {
            [Test]
            public void IsATest() {
                Helper();
                Console.WriteLine();
            }

            public void IsNotATest() {
                Helper();
                Console.WriteLine();
            }

            public void Helper() {
            }
        }
        """;

    [Test]
    public async Task Exclude_tests_skips_test_method_but_still_analyzes_non_test_methods() {
        var expected = Verify.Diagnostic()
            .WithSpan(12, 17, 12, 27)
            .WithArguments("IsNotATest", "1", "- Integration: call to 'Helper'\n", "- Operation: calling API 'WriteLine'\n");
        await TestHelpers.Build(FixtureWithTestAndHelper, expected)
            .WithNet9()
            .WithPackages(new PackageIdentity("NUnit", "4.1.0"))
            .WithEditorConfig("""
                root = true

                [*.cs]
                dotnet_diagnostic.CCD0001.exclude_tests = true
                """)
            .RunAsync();
    }

    [Test]
    public async Task Test_method_is_analyzed_when_exclude_tests_is_not_set() {
        var onTestMethod = Verify.Diagnostic()
            .WithSpan(7, 17, 7, 24)
            .WithArguments("IsATest", "1", "- Integration: call to 'Helper'\n", "- Operation: calling API 'WriteLine'\n");
        var onHelperMethod = Verify.Diagnostic()
            .WithSpan(12, 17, 12, 27)
            .WithArguments("IsNotATest", "1", "- Integration: call to 'Helper'\n", "- Operation: calling API 'WriteLine'\n");
        await TestHelpers.Build(FixtureWithTestAndHelper, onTestMethod, onHelperMethod)
            .WithNet9()
            .WithPackages(new PackageIdentity("NUnit", "4.1.0"))
            .RunAsync();
    }
}
