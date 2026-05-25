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