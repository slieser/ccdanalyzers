namespace CleanCodeDeveloper.Analyzers.NewTests;

[TestFixture]
public class NamespacesFileTests
{
    [Test]
    public async Task Ignores_namespaces_from_file_in_operation() {
        const string input = """
            namespace Bar {
                public class BarClass {
                    public static void Bar() {
                    }
                }
            }

            namespace TheNamespace {
                using System;
                using Bar;
                public class A
                {
                    public void Foo() {
                        BarClass.Bar();
                        Console.WriteLine("test");
                    }
                }
            }
            """;
        await TestHelpers.Build(input)
            .WithNamespacesFile("Bar")
            .WithNet9()
            .RunAsync();
    }

    [Test]
    public async Task Ignores_namespaces_from_file_in_integration() {
        const string input = """
            namespace Bar {
                public class BarClass {
                    public static void Bar() {
                    }
                }
            }

            namespace TheNamespace {
                using Bar;
                using System;
                public class A
                {
                    public void Foo() {
                        BarClass.Bar();
                        DoSomething();
                    }

                    private void DoSomething() {
                        Console.WriteLine("test");
                    }
                }
            }
            """;
        await TestHelpers.Build(input)
            .WithNamespacesFile("Bar")
            .WithNet9()
            .RunAsync();
    }

    [Test]
    public async Task Comments_and_blank_lines_in_namespaces_file_are_ignored() {
        const string input = """
            namespace Bar {
                public class BarClass {
                    public static void Bar() {
                    }
                }
            }

            namespace TheNamespace {
                using System;
                using Bar;
                public class A
                {
                    public void Foo() {
                        BarClass.Bar();
                        Console.WriteLine("test");
                    }
                }
            }
            """;
        const string namespacesFile = """
            # this comment must be ignored
            #Bar.NotIgnored

              Bar

            # trailing comment
            """;
        await TestHelpers.Build(input)
            .WithNamespacesFile(namespacesFile)
            .WithNet9()
            .RunAsync();
    }

    [Test]
    public async Task Multiple_entries_in_namespaces_file_are_all_ignored() {
        const string input = """
            namespace Foo {
                public class FooClass { public static void Do() {} }
            }
            namespace Bar {
                public class BarClass { public static void Do() {} }
            }
            namespace TheNamespace {
                using Foo;
                using Bar;
                public class A {
                    public void M() {
                        FooClass.Do();
                        BarClass.Do();
                    }
                }
            }
            """;
        await TestHelpers.Build(input)
            .WithNamespacesFile("Foo\nBar")
            .WithNet9()
            .RunAsync();
    }

    [Test]
    public async Task EditorConfig_additional_namespaces_extends_namespaces_file() {
        const string input = """
            namespace Extra {
                public class ExtraClass { public static void Do() {} }
            }
            namespace Bar {
                public class BarClass { public static void Do() {} }
            }
            namespace TheNamespace {
                using Extra;
                using Bar;
                public class A {
                    public void M() {
                        ExtraClass.Do();
                        BarClass.Do();
                    }
                }
            }
            """;
        await TestHelpers.Build(input)
            .WithNamespacesFile("Bar")
            .WithEditorConfig("""
                is_global = true
                dotnet_diagnostic.CCD0001.additional_namespaces = Extra
                """)
            .WithNet9()
            .RunAsync();
    }

}
