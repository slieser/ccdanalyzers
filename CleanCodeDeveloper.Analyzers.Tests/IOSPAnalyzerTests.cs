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
                """
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
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_Integration_with_if_statement_calling_a_function() {
            const string test =
                """
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
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_Integration_with_if_statement_containing_an_expression() {
            const string test =
                """
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
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 28).WithArguments("Integration", "- Integration: call to 'Operation1'\n", "- Operation: expression 'x == 42'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_Integration_calls_API() {
            const string test =
                """
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
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 28).WithArguments("Integration", "- Integration: call to 'Operation1'\n", "- Operation: calling API 'ToString'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_Operation_with_API_calls_and_expressions() {
            const string test =
                """
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
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_Operation_with_expression_and_integration_call() {
            const string test =
                """
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
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 27).WithArguments("Operation1", "- Integration: call to 'Operation'\n", "- Operation: expression '1 == 42'\n"),
                Verify.Diagnostic().WithSpan(9, 17, 9, 27).WithArguments("Operation2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 > 42'\n"),
                Verify.Diagnostic().WithSpan(15, 17, 15, 27).WithArguments("Operation3", "- Integration: call to 'Operation'\n", "- Operation: expression '1 >= 42'\n"),
                Verify.Diagnostic().WithSpan(21, 17, 21, 27).WithArguments("Operation4", "- Integration: call to 'Operation'\n", "- Operation: expression '1 < 42'\n"),
                Verify.Diagnostic().WithSpan(27, 17, 27, 27).WithArguments("Operation5", "- Integration: call to 'Operation'\n", "- Operation: expression '1 <= 42'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Nested_expression_is_reported_only_once() {
            const string test =
                """
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
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 27).WithArguments("Operation1", "- Integration: call to 'Operation'\n", "- Operation: expression 'x + 1 == 42'\n"),
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_Integration_with_expression_in_call() {
            const string test =
                """
                class A
                {
                    public void Operation1() {
                        Operation(1 + 5);
                    }
                
                    public void Operation(int x) {
                    }
                }
                """;
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 27).WithArguments("Operation1", "- Integration: call to 'Operation'\n", "- Operation: expression '1 + 5'\n"),
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_Canonical_foreach_loop_in_integration() {
            const string test =
                """
                using System.Collections.Generic;    
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
                }
                """;
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_for_loop_with_expression_in_integration() {
            const string test =
                """
                class A
                {
                    public void Integration() {
                        for(var i = 0; i < 10; i++) {
                            Operation(i);
                        }
                    }
                
                    public void Operation(int x) {
                    }
                }
                """;
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_try_catch_in_integration() {
            const string test =
                """
                class A
                {
                   public void Integration() {
                        try {
                            Operation();
                        }
                        catch {
                            Operation();            
                        }     
                    }
                    public void Operation() {
                    }
                }
                """;
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_try_catch_with_exception_in_integration() {
            const string test =
                """
                using System;
                class A
                {
                    public void Integration5() {
                        try {
                            Operation();
                        }
                        catch (Exception e) {
                            Operation4(e.Message);            
                        }     
                    }
                    public void Operation() {
                    }
                    private void Operation4(string exception) {
                    }
                }
                """;
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_throwing_exceptions_in_integration() {
            const string test =
                """
                using System;
                class A
                {
                    public void Integration() {
                        Operation();
                        throw new Exception("boom");
                    }
                    public void Operation() {
                    }
                }
                """;
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_calling_actions_in_integration() {
            const string test =
                """
                using System;
                class A
                {
                    public void Integration6(Action action) {
                        Operation();
                        action();
                    }
                    public void Operation() {
                    }
                }
                """;
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_mixing_own_Invoke_with_API_call_in_integration() {
            const string test =
                """
                using System;
                class A
                {
                    public void Integration() {
                        Invoke();
                        Console.WriteLine();
                    }
                    public void Invoke() {
                    }
                }
                """;
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(4, 17, 4, 28).WithArguments("Integration", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_expression_in_for_loop_block_in_integration() {
            const string test =
                """
                class A
                {
                    public void Integration() {
                        for(var i = 0; i < 10; i++) {
                            Operation(i + 1);
                        }
                    }
                
                    public void Operation(int x) {
                    }
                }
                """;
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 28).WithArguments("Integration", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Allowed_local_method_call_in_integration() {
            const string test =
                """
                class A
                {
                    public void Integration() {
                        void LocalOperation() {
                        }
                        Operation();
                    }
                
                    public void Operation() {
                    }
                }
                """;
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_local_method_call__with_expression_in_integration() {
            const string test =
                """
                class A
                {
                    public void Integration() {
                        void LocalOperation() {
                            var x = 4 + 2;
                        }
                        Operation();
                    }
                
                    public void Operation() {
                    }
                }
                """;
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(3, 17, 3, 28).WithArguments("Integration", "- Integration: call to 'Operation'\n", "- Operation: expression '4 + 2'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Multiple_integration_calls_are_listed_only_once_in_message() {
            const string test =
                """
                using System.Collections.Generic;    
                class A
                {
                    public void Integration() {
                         var i = 2;
                         Operation(i + 1);
                         Operation(i + 2);
                    }
                
                    public void Operation(int x) {
                    }
                }
                """;
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(4, 17, 4, 28).WithArguments("Integration", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n- Operation: expression 'i + 2'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Multiple_expressions_are_listed_only_once_in_message() {
            const string test =
                """
                using System.Collections.Generic;    
                class A
                {
                    public void Integration() {
                         var i = 2;
                         Operation(i + 1);
                         Operation(i + 1);
                    }
                
                    public void Operation(int x) {
                    }
                }
                """;
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(4, 17, 4, 28).WithArguments("Integration", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n")
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }


        [Fact]
        public async Task Invocation_of_ConfigureAwait_does_not_create_message() {
            const string input = 
                """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                public class Class2
                {
                    public async Task<int> GetNumber_IntegrationOnly()
                    {
                        var list = GetList();
                        var number = await FilterNumberAsync(list);
                        return number;
                    }
                    /// <remarks>
                    /// Warning CCD0001 Method 'GetNumber_IntegrationOnlyWithConfigureAwait' mixes integration with operation.
                    ///   Integration: call to 'GetList'
                    ///   Integration: call to 'FilterNumberAsync'
                    ///   Operation: calling API 'ConfigureAwait'
                    /// </remarks>
                    public async Task<int> GetNumber_IntegrationOnlyWithConfigureAwait()
                    {
                        var list = GetList();
                        var number = await FilterNumberAsync(list).ConfigureAwait(continueOnCapturedContext: false);
                        return number;
                    }
                    private Task<int> FilterNumberAsync(IReadOnlyList<int> list)
                    {
                        int value()
                        {
                            if (list.Count > 0)
                            {
                                return list[list.Count - 1];
                            }
                            return 0;
                        }
                        var task = Task.Run(value);
                        return task;
                    }
                    private static IReadOnlyList<int> GetList()
                    {
                        return new List<int>() { 1, 2, 3, 4 };
                    }
                }
                """;
            DiagnosticResult[] expected = {
            };
            await Verify.VerifyAnalyzerAsync(input, expected);
        }
    }
}