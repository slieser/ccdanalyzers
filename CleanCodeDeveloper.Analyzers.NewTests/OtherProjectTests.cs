using System.Collections.Immutable;
using IncludedProject;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

public class OtherProjectTests
{
    [Test, Explicit("Roslyn has no solution for scanning multiple projects")]
    public async Task Should_not_violate_IOSP() {
        const string input = """
            using IncludedProject;
            
            namespace examples.nunit.other;
            
            public class Usage(TheInterface theInterface, TheImplementation theImplementation)
            {
                public void UseInjectedInterfaceFromOtherProject() {
                    theInterface.DoSomething();
                    Integration();
                }
            
                public void UseInjectedImplementationFromOtherProject() {
                    theImplementation.DoSomething();
                    Integration();
                }
            
                private void Integration() {
                }
            }
            """;

        var myProject = MetadataReference.CreateFromFile(typeof(TheInterface).Assembly.Location);

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input },
                AdditionalReferences = { myProject }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        await cSharpAnalyzerTest.RunAsync();
    }
}
