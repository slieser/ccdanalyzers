using IncludedProject;

namespace examples.nunit.other;

public class Usage(TheInterface theInterface, TheImplementation theImplementation)
{
    public void UseInjectedInterfaceFromOtherProject() {
        theInterface.DoSomething();
        Integration();
    }

    public void UseInjectedImplementationFromOtherProject() {
        theImplementation.DoSomething();
        Integration();
    }

    private void Integration() {
    }
}
