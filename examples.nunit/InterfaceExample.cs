namespace examples.nunit;

public class Usage(IInterface instance, Implementation implementation)
{
    public void UseInterface(IInterface i) {
        i.DoSomething();
        Integration();
        StaticClass.DoSomething();
    }

    public void UseImplementation(Implementation i) {
        i.DoSomething();
        Integration();
        StaticClass.DoSomething();
    }

    public void UseInjectedInterface() {
        instance.DoSomething();
        Integration();
        StaticClass.DoSomething();
    }

    public void UseInjectedImplementation() {
        implementation.DoSomething();
        Integration();
        StaticClass.DoSomething();
    }

    private void Integration() {
    }
}

public interface IInterface
{
    void DoSomething();
}

public class Implementation : IInterface
{
    public void DoSomething() {
    }
}

public static class StaticClass
{
    public static void DoSomething() {
    }
}