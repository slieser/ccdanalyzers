namespace examples.nunit;

public class OverrideExample : MyBaseClass
{
    public override void DoSomething() {
        base.DoSomething();
        var _ = 5 + 4;
    }
}

public class MyBaseClass
{
    public virtual void DoSomething() { }
}

public class MyObject
{
    public override string ToString() {
        return $"{base.ToString()} + {Integration()}";
    }

    private string Integration() {
        return "hahaha";
    }
}