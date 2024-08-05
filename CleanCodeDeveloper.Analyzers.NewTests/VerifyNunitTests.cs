using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

public class VerifyNunitTests
{
    [Test]
    public async Task Should_not_violate_IOSP() {
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

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
                .AddPackages([ 
                    new PackageIdentity("NUnit", "4.1.0"),
                    new PackageIdentity("Verify.NUnit", "26.1.6"),
                ]) 
        };
        await cSharpAnalyzerTest.RunAsync();
    }
}
