namespace examples.nunit;

public class MethodNames
{
    public void DoSomething() {
        var i = new[] {1, 2, 3}.ToList();
        ToList();
    }

    public void ToList() {
    }
}