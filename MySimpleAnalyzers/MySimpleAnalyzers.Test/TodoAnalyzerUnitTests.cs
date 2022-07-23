using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = MySimpleAnalyzers.Test.CSharpCodeFixVerifier<
    MySimpleAnalyzers.TodoAnalyzer,
    MySimpleAnalyzers.TodoAnalyzersCodeFixProvider>;

namespace MySimpleAnalyzers.Test
{
    [TestClass]
    public class TodoAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
// Todo: next task.
    namespace ConsoleApplication1
    {
        class MyClass
        {   
        }
    }";
            var fixedCode = @"
// Todo: next task. JIRA-0000
    namespace ConsoleApplication1
    {
        class MyClass
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic(TodoAnalyzer.RuleDescriptor).WithLocation(2, 4);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedCode);
        }
    }
}
