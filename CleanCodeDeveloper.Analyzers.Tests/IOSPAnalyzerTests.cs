using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CleanCodeDeveloper.Analyzers.IOSPAnalyzer>;

namespace CleanCodeDeveloper.Analyzers.Tests
{
    public class IOSPAnalyzerTests
    {
        [Fact]
        public async Task Allowed_Integration_only() {
            const string test = 
@"class A
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
}";
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
        
        [Fact]
        public async Task Allowed_Integration_with_if_statement_calling_a_function() {
            const string test = 
@"class A
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
}";
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
        
        [Fact]
        public async Task Not_allowed_Integration_with_if_statement_containing_an_expression() {
            const string test = 
@"class A
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
}";
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 28).WithArguments("Integration", "  Integration: call to 'Operation1'\n", "  Operation: expression 'x == 42'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
        
        [Fact]
        public async Task Not_allowed_Integration_calls_API() {
            const string test = 
@"class A
{
    public void Integration()
    {
        Operation1();
        var s = 42.ToString();
    }

    public void Operation1() {
    }
}";
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 28).WithArguments("Integration", "  Integration: call to 'Operation1'\n", "  Operation: calling API 'ToString'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    
        [Fact]
        public async Task Allowed_Operation_with_API_calls_and_expressions() {
            const string test = 
@"class A
{
    public void Operation() {
        var x = 5;
        if(x + 1 == 42) {
            System.Console.WriteLine(x);
        }
    }
}";
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    
        [Fact]
        public async Task Not_allowed_Operation_with_expression_and_integration_call() {
            const string test = 
@"class A
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
}";
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan( 3, 17,  3, 27).WithArguments("Operation1", "  Integration: call to 'Operation'\n", "  Operation: expression '1 == 42'\n"),
                Verify.Diagnostic().WithSpan( 9, 17,  9, 27).WithArguments("Operation2", "  Integration: call to 'Operation'\n", "  Operation: expression '1 > 42'\n"),
                Verify.Diagnostic().WithSpan(15, 17, 15, 27).WithArguments("Operation3", "  Integration: call to 'Operation'\n", "  Operation: expression '1 >= 42'\n"),
                Verify.Diagnostic().WithSpan(21, 17, 21, 27).WithArguments("Operation4", "  Integration: call to 'Operation'\n", "  Operation: expression '1 < 42'\n"),
                Verify.Diagnostic().WithSpan(27, 17, 27, 27).WithArguments("Operation5", "  Integration: call to 'Operation'\n", "  Operation: expression '1 <= 42'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    
        [Fact]
        public async Task Nested_expression_is_reported_only_once() {
            const string test = 
@"class A
{
    public void Operation1() {
        var x = 5;
        if(x + 1 == 42) {
            Operation();
        }
    }

    public void Operation() {
    }
}";
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan( 3, 17,  3, 27).WithArguments("Operation1", "  Integration: call to 'Operation'\n", "  Operation: expression 'x + 1 == 42'\n"),
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    
        [Fact]
        public async Task Not_allowed_Integration_with_expression_in_call() {
            const string test = 
@"class A
{
    public void Operation1() {
        Operation(1 + 5);
    }

    public void Operation(int x) {
    }
}";
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan( 3, 17,  3, 27).WithArguments("Operation1", "  Integration: call to 'Operation'\n", "  Operation: expression '1 + 5'\n"),
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    
        [Fact]
        public async Task Allowed_Canonical_foreach_loop_in_integration() {
            const string test = 
@"using System.Collections.Generic;    
class A
{
    public void Integration() {
        var result = Operation1();
        foreach(var i in result) {
            Operation2(i);
        }
    }

    public IEnumerable<int> Operation1() {
        return new[]{1, 2, 3};
    }

    public void Operation2(int x) {
    }
}";
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_for_loop_in_integration() {
            const string test = 
@"using System.Collections.Generic;    
class A
{
    public void Integration() {
        for(var i = 0; i < 10; i++) {
            Operation(i);
        }
    }

    public void Operation(int x) {
    }
}";
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_expression_in_for_loop_block_in_integration() {
            const string test = 
@"using System.Collections.Generic;    
class A
{
    public void Integration() {
        for(var i = 0; i < 10; i++) {
            Operation(i + 1);
        }
    }

    public void Operation(int x) {
    }
}";
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(4, 17, 4, 28).WithArguments("Integration", "  Integration: call to 'Operation'\n", "  Operation: expression 'i + 1'\n")            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}