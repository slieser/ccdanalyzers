using Microsoft.Extensions.Logging;

namespace examples.nunit;

public class LoggerExample(ILogger<LoggerExample> logger)
{
    public void DoSomething() {
        logger.LogInformation(nameof(DoSomething));
        Integration();
    }

    private void Integration() {
    }
}