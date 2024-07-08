namespace examples.nunit;

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