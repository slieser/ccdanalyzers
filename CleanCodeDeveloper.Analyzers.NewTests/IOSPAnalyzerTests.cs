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
        await TestHelpers.Build(input).RunAsync();
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
        await TestHelpers.Build(input).RunAsync();
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
        await TestHelpers.Build(input, expected).RunAsync();
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
        await TestHelpers.Build(input, expected).RunAsync();
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
        await TestHelpers.Build(input).RunAsync();
    }

    [TestCase("==")]
    [TestCase(">")]
    [TestCase(">=")]
    [TestCase("<")]
    [TestCase("<=")]
    public async Task Not_allowed_Operation_with_comparison_expression_and_integration_call(string op) {
        var input = $$"""
            class A
            {
                public void OperationUnderTest() {
                    if(1 {{op}} 42) {
                        Operation();
                    }
                }

                public void Operation() {
                }
            }
            """;
        var expected = Verify.Diagnostic()
            .WithSpan(3, 17, 3, 35)
            .WithArguments("OperationUnderTest", "2", "- Integration: call to 'Operation'\n", $"- Operation: expression '1 {op} 42'\n");
        await TestHelpers.Build(input, expected).RunAsync();
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
        var expected = Verify.Diagnostic()
            .WithSpan(3, 17, 3, 27)
            .WithArguments("Operation1", "2", "- Integration: call to 'Operation'\n", "- Operation: expression 'x + 1 == 42'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Not_allowed_Integration_with_expression_in_call() {
        const string input = """
            class A
            {
                public void Operation1() {
                    Operation(1 + 5);
                }

                public void Operation(int x) {
                }
            }
            """;
        var expected = Verify.Diagnostic()
            .WithSpan(3, 17, 3, 27)
            .WithArguments("Operation1", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 + 5'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Allowed_Canonical_foreach_loop_in_integration() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Allowed_for_loop_with_expression_in_integration() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Allowed_try_catch_in_integration() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Allowed_try_catch_with_exception_in_integration() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Allowed_throwing_exceptions_in_integration() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Allowed_calling_actions_in_integration() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Allowed_calling_func_in_integration() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Not_allowed_calling_actions_in_operation() {
        const string input = """
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
        var expected = new[] {
            Verify.Diagnostic().WithSpan(4, 17, 4, 27).WithArguments("Operation5", "1", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n"),
            Verify.Diagnostic().WithSpan(8, 17, 8, 27).WithArguments("Operation6", "1", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n")
        };
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Not_allowed_calling_func_in_operation() {
        const string input = """
            using System;
            class A
            {
                public void Operation5(Func<int> func) {
                    Console.WriteLine();
                    var i = func();
                }
            }
            """;
        var expected = Verify.Diagnostic()
            .WithSpan(4, 17, 4, 27)
            .WithArguments("Operation5", "1", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Not_allowed_mixing_own_Invoke_with_API_call_in_integration() {
        const string input = """
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
        var expected = Verify.Diagnostic()
            .WithSpan(4, 17, 4, 28)
            .WithArguments("Integration", "1", "- Integration: call to 'Invoke'\n", "- Operation: calling API 'WriteLine'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Not_allowed_expression_in_for_loop_block_in_integration() {
        const string input = """
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
        var expected = Verify.Diagnostic()
            .WithSpan(3, 17, 3, 28)
            .WithArguments("Integration", "2", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Allowed_local_method_call_in_integration() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Not_allowed_local_method_call_with_expression_in_integration() {
        const string input = """
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
        var expected = Verify.Diagnostic()
            .WithSpan(3, 17, 3, 28)
            .WithArguments("Integration", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '4 + 2'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Multiple_integration_calls_are_listed_only_once_in_message() {
        const string input = """
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
        var expected = Verify.Diagnostic()
            .WithSpan(4, 17, 4, 28)
            .WithArguments("Integration", "4", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n- Operation: expression 'i + 2'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Multiple_expressions_are_listed_only_once_in_message() {
        const string input = """
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
        var expected = Verify.Diagnostic()
            .WithSpan(4, 17, 4, 28)
            .WithArguments("Integration", "2", "- Integration: call to 'Operation'\n", "- Operation: expression 'i + 1'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Invocation_of_ConfigureAwait_does_not_create_message() {
        const string input = """
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
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Diagnostic_respects_min_metric_setting() {
        const string input = """
            using System;
            class A
            {
                public void Method1() {
                    Operation();
                    Console.WriteLine();
                }

                public void Method2() {
                    Operation(1 + 5);
                }

                public void Operation() {
                }
                public void Operation(int x) {
                }
            }
            """;
        var expected = Verify.Diagnostic()
            .WithSpan(9, 17, 9, 24)
            .WithArguments("Method2", "2", "- Integration: call to 'Operation'\n", "- Operation: expression '1 + 5'\n");
        await TestHelpers.Build(input, expected)
            .WithEditorConfig("""
                root = true

                [*.cs]
                dotnet_diagnostic.CCD0001.min_metric = 2
                """)
            .RunAsync();
    }

    [Test]
    public async Task Diagnostic_respects_min_metric_setting_suppresses_all() {
        const string input = """
            using System;
            class A
            {
                public void Method1() {
                    Operation();
                    Console.WriteLine();
                }

                public void Method2() {
                    Operation(1 + 5);
                }

                public void Operation() {
                }
                public void Operation(int x) {
                }
            }
            """;
        await TestHelpers.Build(input)
            .WithEditorConfig("""
                root = true

                [*.cs]
                dotnet_diagnostic.CCD0001.min_metric = 3
                """)
            .RunAsync();
    }

    [TestCase("0")]
    [TestCase("-5")]
    public async Task Min_metric_below_one_is_clamped_to_default(string minMetric) {
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
        await TestHelpers.Build(input, expected)
            .WithEditorConfig($"""
                root = true

                [*.cs]
                dotnet_diagnostic.CCD0001.min_metric = {minMetric}
                """)
            .RunAsync();
    }

    [Test]
    public async Task Constructor_with_integration_and_expression_is_flagged() {
        const string input = """
            class A
            {
                public A() {
                    var x = 1 + 2;
                    Init();
                }

                private void Init() {
                }
            }
            """;
        var expected = Verify.Diagnostic()
            .WithSpan(3, 12, 3, 13)
            .WithArguments(".ctor", "2", "- Integration: call to 'Init'\n", "- Operation: expression '1 + 2'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Async_method_with_await_and_expression_is_flagged() {
        const string input = """
            using System.Threading.Tasks;
            class A
            {
                public async Task RunAsync() {
                    var x = 1 + 2;
                    await DoAsync();
                }

                private Task DoAsync() => Task.CompletedTask;
            }
            """;
        var expected = Verify.Diagnostic()
            .WithSpan(4, 23, 4, 31)
            .WithArguments("RunAsync", "2", "- Integration: call to 'DoAsync'\n", "- Operation: expression '1 + 2'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Async_method_with_only_awaits_is_clean() {
        const string input = """
            using System.Threading.Tasks;
            class A
            {
                public async Task RunAsync() {
                    await OneAsync();
                    await TwoAsync();
                }

                private Task OneAsync() => Task.CompletedTask;
                private Task TwoAsync() => Task.CompletedTask;
            }
            """;
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Expression_bodied_method_with_integration_call_is_clean() {
        const string input = """
            class A
            {
                public int Run() => Compute();
                private int Compute() => 42;
            }
            """;
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Expression_bodied_method_with_expression_only_is_clean() {
        const string input = """
            class A
            {
                public int Add(int a, int b) => a + b;
            }
            """;
        await TestHelpers.Build(input).RunAsync();
    }

    [Test]
    public async Task Expression_bodied_method_with_integration_and_expression_is_flagged() {
        const string input = """
            class A
            {
                public int Run() => Compute() + 1;
                private int Compute() => 42;
            }
            """;
        var expected = Verify.Diagnostic()
            .WithSpan(3, 16, 3, 19)
            .WithArguments("Run", "2", "- Integration: call to 'Compute'\n", "- Operation: expression 'Compute() + 1'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Property_getter_with_block_body_mixing_integration_and_expression_is_flagged() {
        const string input = """
            class A
            {
                public int Value
                {
                    get
                    {
                        var x = 1 + 2;
                        return Compute();
                    }
                }
                private int Compute() => 42;
            }
            """;
        var expected = Verify.Diagnostic()
            .WithSpan(5, 9, 5, 12)
            .WithArguments("get_Value", "2", "- Integration: call to 'Compute'\n", "- Operation: expression '1 + 2'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task Methods_with_same_name_from_different_types_are_counted_separately() {
        const string input = """
            class A
            {
                public void Run() {
                    B.DoIt();
                    C.DoIt();
                    var s = 42.ToString();
                }
            }

            class B { public static void DoIt() { } }
            class C { public static void DoIt() { } }
            """;
        // integrations: 2 distinct keys (B.DoIt, C.DoIt) – display dedups to "DoIt"
        // operations: 1 (ToString). integrations(2) > operations(1) → metric = 1 + 0 = 1
        var expected = Verify.Diagnostic()
            .WithSpan(3, 17, 3, 20)
            .WithArguments("Run", "1", "- Integration: call to 'DoIt'\n", "- Operation: calling API 'ToString'\n");
        await TestHelpers.Build(input, expected).RunAsync();
    }

    [Test]
    public async Task For_loop_initializer_is_not_treated_as_canonical_exempt() {
        // The Condition position is the only one that's exempt; the canonical `i < 10` here is exempt.
        const string input = """
            class A
            {
                public void Integration() {
                    for(var i = 0; i < 10; i++) {
                        Operation();
                    }
                }
                private void Operation() { }
            }
            """;
        await TestHelpers.Build(input).RunAsync();
    }
}
