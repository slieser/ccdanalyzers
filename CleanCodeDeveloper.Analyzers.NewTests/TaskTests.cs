using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

public class TaskTests
{
    [Test]
    public async Task Calling_Task_Run_should_not_violate_IOSP() {
        const string input = """
            using System.Threading.Tasks;
            public class TaskExamples
            {
                public void DoSomething_ok() {
                    Task.Run(() => {
                        var _ = 5 + 4;
                    });
                }
            
                public void DoSomething_fail() {
                    Task.Run(() => {
                        var _ = Calc();
                    });
                }
            
                private static int Calc() {
                    return 5 + 4;
                }
            }
            """;

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input }
            } 
        };
        await cSharpAnalyzerTest.RunAsync();
    }
}
