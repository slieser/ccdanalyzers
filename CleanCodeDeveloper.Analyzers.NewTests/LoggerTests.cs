using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

public class LoggerTests
{
    [Test]
    public async Task Should_not_violate_IOSP() {
        const string input = """
            using Microsoft.Extensions.Logging;
            
            namespace examples.nunit;
            
            public class LoggerExample(ILogger<LoggerExample> logger)
            {
                public void DoSomething() {
                    logger.LogInformation(nameof(DoSomething));
                    Integration();
                }
            
                private void Integration() {
                }
            }
            """;

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
                .AddPackages([ new PackageIdentity("Microsoft.Extensions.Logging.Abstractions", "8.0.1"),]) 
        };
        await cSharpAnalyzerTest.RunAsync();
    }
}
