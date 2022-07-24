namespace CodeAnalyzerDemo;

internal class DemoClass
{
    private readonly string[] _values = { "dummy" };
    public void DoWork(int n)
    {
        // TODO:  CSI-1123 do something later.
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

                foreach (var s1 in _values)
                {
                    Console.WriteLine(s1);
                }
            }
        }
    }
}