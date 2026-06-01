using Moq;

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