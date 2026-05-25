using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class LoggerTests
{
    [Test]
    public async Task ILogger_calls_are_ignored_when_namespace_listed() {
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
        await TestHelpers.Build(input)
            .WithDefaultNamespaces()
            .WithNet9()
            .WithPackages(new PackageIdentity("Microsoft.Extensions.Logging.Abstractions", "8.0.1"))
            .RunAsync();
    }
}
