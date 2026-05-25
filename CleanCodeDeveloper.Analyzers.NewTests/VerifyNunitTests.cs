using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class VerifyNunitTests
{
    [Test]
    public async Task Verify_calls_are_ignored_when_namespace_listed() {
        const string input = """
            using NUnit.Framework;
            using VerifyNUnit;
            using VerifyTests;
            using static global::VerifyNUnit.Verifier;
            using System.Threading.Tasks;

            namespace examples.nunit;

            [TestFixture]
            public class VerifyTests
            {
                [Test]
                public async Task Test_something() {
                    var result = DoSomething();
                    await Verify(result);
                }

                private string DoSomething() {
                    return "This is the result";
                }
            }
            """;
        await TestHelpers.Build(input)
            .WithDefaultNamespaces()
            .WithNet9()
            .WithPackages(
                new PackageIdentity("NUnit", "4.1.0"),
                new PackageIdentity("Verify.NUnit", "26.1.6"))
            .RunAsync();
    }
}
