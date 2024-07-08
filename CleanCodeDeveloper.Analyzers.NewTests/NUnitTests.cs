using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

public class NUnitTests
{
    [Test]
    public async Task Should_not_violate_IOSP() {
        const string input = """
            using NUnit.Framework;
            [TestFixture]
            public class ExampleNUnitTests
            {
               [Test]
               public void Should_not_violate_IOSP() {
                   var sut = new Sut();
                   Assert.That(sut.Add(1, 2), Is.EqualTo(3));      
               }
            }

            public class Sut
            {
               public int Add(int a, int b) => a + b;
            }
            """;

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
                .AddPackages([
                    new PackageIdentity("NUnit", "4.1.0"),
                ]) 
        };
        await cSharpAnalyzerTest
            .RunAsync();
    }
}
