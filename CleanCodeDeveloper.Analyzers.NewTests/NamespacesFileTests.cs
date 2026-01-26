using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class NamespacesFileTests
{
    [Test]
    public async Task Ignores_namespaces_from_file() {
        const string input = """
            using System;
            class A
            {
                public void Integration() {
                    Operation();
                    Console.WriteLine("test");
                }

                public void Operation() {
                }
            }
            """;

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input },
                AdditionalFiles = { ("namespaces.txt", "System") }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };

        await cSharpAnalyzerTest.RunAsync();
    }
}
