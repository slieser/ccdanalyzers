using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<CleanCodeDeveloper.Analyzers.IOSPAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;


namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class IOSPAnalyzerTests
{
    [Test]
    public async Task Allowed_Integration_only() {
        const string input = """
             class A
             {
                 public void Integration()
                 {
                     Operation1();
                     Operation2();
                 }
             
                 public void Operation1() {
                 }
             
                 public void Operation2() {
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

    [Test]
    public async Task Allowed_Integration_with_if_statement_calling_a_function() {
        const string input = """
             class A
             {
                 public void Integration()
                 {
                     if(IsCorrect(42)) {
                         Operation1();
                     }
                 }
             
                 public void Operation1() {
                 }
             
                 public bool IsCorrect(int x) {
                     return x == 42;
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
    
    [Test]
    public async Task Not_allowed_Integration_with_if_statement_containing_an_expression() {
        const string input = """
             class A
             {
                 public void Integration()
                 {
                     var x = 5;
                     if(x == 42) {
                         Operation1();
                     }
                 }
             
                 public void Operation1() {
                 }
             }
             """;

        var expected = Verify.Diagnostic()
            .WithSpan(3, 17, 3, 28)
            .WithArguments("Integration", "2", "- Integration: call to 'Operation1'\n", "- Operation: expression 'x == 42'\n");
        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input }
            }, ExpectedDiagnostics = { expected }
        };
        await cSharpAnalyzerTest.RunAsync();
    }
    
    [Test]
    public async Task Not_allowed_Integration_calls_API() {
        const string input = """
             class A
             {
                 public void Integration()
                 {
                     Operation1();
                     var s = 42.ToString();
                 }
             
                 public void Operation1() {
                 }
             }
             """;

        var expected = Verify.Diagnostic()
            .WithSpan(3, 17, 3, 28)
            .WithArguments("Integration", "1", "- Integration: call to 'Operation1'\n", "- Operation: calling API 'ToString'\n");

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input }
            }, ExpectedDiagnostics = { expected }
        };
        await cSharpAnalyzerTest.RunAsync();
    }
    
    [Test]
    public async Task Allowed_Operation_with_API_calls_and_expressions() {
        const string input = """
             class A
             {
                 public void Operation() {
                     var x = 5;
                     if(x + 1 == 42) {
                         System.Console.WriteLine(x);
                     }
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
    
    [Test]
    public async Task Not_allowed_Operation_with_expression_and_integration_call() {
        const string input = """
            class A
            {
                public void Operation1() {
                    if(1 == 42) {
                        Operation();
                    }
                }
            
                public void Operation2() {
                    if(1 > 42) {
                        Operation();
                    }
                }
            
                public void Operation3() {
                    if(1 >= 42) {
                        Operation();
                    }
                }
            
                public void Operation4() {
                    if(1 < 42) {
                        Operation();
                    }
                }
            
                public void Operation5() {
                    if(1 <= 42) {
                        Operation();
                    }
                }
            
                public void Operation() {
                }
            }
            """;

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input }
            }, ExpectedDiagnostics = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 27).WithArguments("Operation1", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 == 42'\n"),
                Verify.Diagnostic().WithSpan(9, 17, 9, 27).WithArguments("Operation2", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 > 42'\n"),
                Verify.Diagnostic().WithSpan(15, 17, 15, 27).WithArguments("Operation3", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 >= 42'\n"),
                Verify.Diagnostic().WithSpan(21, 17, 21, 27).WithArguments("Operation4", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 < 42'\n"),
                Verify.Diagnostic().WithSpan(27, 17, 27, 27).WithArguments("Operation5", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 <= 42'\n")
            }
        };
        await cSharpAnalyzerTest.RunAsync();
    }
    
    [Test]
    public async Task Nested_expression_is_reported_only_once() {
        const string input = """
            class A
            {
                public void Operation1() {
                    var x = 5;
                    if(x + 1 == 42) {
                        Operation();
                    }
                }
            
                public void Operation() {
                }
            }
            """;

        var cSharpAnalyzerTest = new CSharpAnalyzerTest<IOSPAnalyzer, DefaultVerifier> {
            TestState = {
                Sources = { input }
            }, ExpectedDiagnostics = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 27).WithArguments("Operation1", "2", "- Integration: call to 'Operation'\n", "- Operation: expression 'x + 1 == 42'\n"),
            }
        };
        await cSharpAnalyzerTest.RunAsync();
    }

}