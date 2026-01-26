using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class NamespacesFileTests
{
    [Test]
    public async Task Ignores_namespaces_from_file_in_operation() {
        const string input = """
            namespace Bar {
                public class BarClass {
                    public static void Bar() {
                    }
                }
            }

            namespace TheNamespace {
                using System;
                using Bar;
                public class A
                {
                    public void Foo() {
                        BarClass.Bar();
                        Console.WriteLine("test");
                    }
                }
            }
            """;

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input },
                AdditionalFiles = { ("namespaces.txt", "Bar") }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };

        await cSharpAnalyzerTest.RunAsync();
    }

    [Test]
    public async Task Ignores_namespaces_from_file_in_integration() {
        const string input = """
            namespace Bar {
                public class BarClass {
                    public static void Bar() {
                    }
                }
            }

            namespace TheNamespace {
                using Bar;
                using System;
                public class A
                {
                    public void Foo() {
                        BarClass.Bar();
                        DoSomething();
                    }
                    
                    private void DoSomething() {
                        Console.WriteLine("test");
                    }
                }
            }
            """;

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input },
                AdditionalFiles = { ("namespaces.txt", "Bar") }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };

        await cSharpAnalyzerTest.RunAsync();
    }
}
