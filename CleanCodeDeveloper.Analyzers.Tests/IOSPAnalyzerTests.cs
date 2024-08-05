using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CleanCodeDeveloper.Analyzers.IOSPAnalyzer>;

namespace CleanCodeDeveloper.Analyzers.Tests
{
    public class IOSPAnalyzerTests
    {

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
                Verify.Diagnostic().WithSpan(3, 17, 3, 27).WithArguments("Operation1", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 + 5'\n"),
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
        public async Task Allowed_calling_func_in_integration() {
            const string test =
                """
                using System;
                class A
                {
                    public void Integration(Func<string> func) {
                        Operation();
                        func();
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
        public async Task Not_allowed_calling_actions_in_operation() {
            const string test =
                """
                using System;
                class A
                {
                    public void Operation5(Action action) {
                        Console.WriteLine();
                        action();
                    }
                    public void Operation6(Action<string> action) {
                        Console.WriteLine();
                        action("Hi");
                    }
                }
                """;
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(4, 17, 4, 27).WithArguments("Operation5", "1", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n"),
                Verify.Diagnostic().WithSpan(8, 17, 8, 27).WithArguments("Operation6", "1", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n")            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Not_allowed_calling_func_in_operation() {
            const string test =
                """
                using System;
                class A
                {
                    public void Operation5(Func<int> func) {
                        Console.WriteLine();
                        var i = func();
                    }
                }
                """;
            DiagnosticResult[] expected = {
                Verify.Diagnostic().WithSpan(4, 17, 4, 27).WithArguments("Operation5", "1", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n")
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
                Verify.Diagnostic().WithSpan(4, 17, 4, 28).WithArguments("Integration", "1", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n")
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
                Verify.Diagnostic().WithSpan(3, 17, 3, 28).WithArguments("Integration", "2", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n")
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
                Verify.Diagnostic().WithSpan(3, 17, 3, 28).WithArguments("Integration", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '4 + 2'\n")
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
                Verify.Diagnostic().WithSpan(4, 17, 4, 28).WithArguments("Integration", "4", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n- Operation: expression 'i + 2'\n")
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
                Verify.Diagnostic().WithSpan(4, 17, 4, 28).WithArguments("Integration", "2", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n")
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