using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CleanCodeDeveloper.Analyzers.NewTests;

public class OverrideTests
{
    [Test]
    public async Task Override_of_own_method_should_not_violate_IOSP() {
        const string input = """
            public class OverrideExample : MyBaseClass
            {
                public override void DoSomething() {
                    base.DoSomething();
                    var _ = 5 + 4;
                }
            }
            
            public class MyBaseClass
            {
                public virtual void DoSomething() { }
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
    public async Task Override_of_foreign_method_should_not_violate_IOSP() {
        const string input = """
            public class MyObject
            {
                public override string ToString() {
                    return $"{base.ToString()} + {Integration()}";
                }
            
                private string Integration() {
                    return "hahaha";
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
