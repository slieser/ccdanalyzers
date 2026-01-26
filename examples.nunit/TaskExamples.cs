namespace examples.nunit;

public class TaskExamples
{
    public void DoSomething_ok() {
        Task.Run(() => {
            _ = 5 + 4;
        });
    }

    public void DoSomething_fail() {
        Task.Run(() => {
            _ = Calc();
        });
    }

    private static int Calc() {
        return 5 + 4;
    }
}
