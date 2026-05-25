using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class NUnitTests
{
    [Test]
    public async Task NUnit_Assert_is_ignored_when_namespace_listed() {
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
        await TestHelpers.Build(input)
            .WithDefaultNamespaces()
            .WithNet9()
            .WithPackages(new PackageIdentity("NUnit", "4.1.0"))
            .RunAsync();
    }
}
