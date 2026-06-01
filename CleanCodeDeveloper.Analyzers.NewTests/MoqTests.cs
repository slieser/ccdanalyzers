using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class MoqTests
{
    [Test]
    public async Task Moq_Setup_and_Returns_are_ignored_when_namespace_listed() {
        const string input = """
            using Moq;
            using NUnit.Framework;
            
            namespace examples.nunit;
            
            public interface IService
            {
                int Add(int a, int b); 
            }
            
            [TestFixture]
            public class MoqTestExample
            {
                [Test]
                public void Add_ReturnsCorrectResult() {
                    var serviceMock = new Mock<IService>();
                    serviceMock.Setup(s => s.Add(2, 3)).Returns(5);
            
                    var result = serviceMock.Object.Add(2, 3);
            
                    Assert.That(result, Is.EqualTo(5));
                }
            }
            """;
        await TestHelpers.Build(input)
            .WithDefaultNamespaces()
            .WithNet9()
            .WithPackages(new PackageIdentity("NUnit", "4.1.0"))
            .WithPackages(new PackageIdentity("Moq", "4.20.72"))
            .RunAsync();
    }
}
