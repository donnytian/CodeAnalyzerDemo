using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = MySimpleAnalyzers.Test.CSharpCodeFixVerifier<
    MySimpleAnalyzers.DeepLoopAnalyzer,
    MySimpleAnalyzers.TodoAnalyzersCodeFixProvider>;

namespace MySimpleAnalyzers.Test
{
    [TestClass]
    public class DeepLoopAnalyzersUnitTest
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
using System;

internal class OrderService
{
    private readonly string[] _values = { };
    public void DoWork(int n)
    {
        for (var i = 0; i < n; i++)
        {
            Console.WriteLine(n);

            foreach (var value in _values)
            {
                Console.WriteLine(value);

                var j = i;
                while (j < 100)
                {
                    foreach (var s in _values)
                    {
                        Console.WriteLine(s);
                    }

                    Console.WriteLine(j);
                    j++;
                }

                foreach (var s in _values)
                {
                    Console.WriteLine(s);
                }
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic(DeepLoopAnalyzer.RuleDescriptor).WithLocation(20, 21);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
