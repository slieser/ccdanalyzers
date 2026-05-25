namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class InterfacesTests
{
    [Test]
    public async Task Method_called_via_local_interface_counts_as_integration() {
        const string input = """
            public interface IGreeter
            {
                void Greet();
            }

            public class Greeter : IGreeter
            {
                public void Greet() { }
            }

            public class Caller
            {
                private readonly IGreeter _greeter;
                public Caller(IGreeter greeter) { _greeter = greeter; }

                public void Run() {
                    _greeter.Greet();
                    DoMore();
                }

                private void DoMore() { }
            }
            """;
        await TestHelpers.Build(input).RunAsync();
    }
}
