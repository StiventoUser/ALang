using System;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Compiler compiler = Compiler.Instance;
            compiler.DoTask(args);
        }
    }
}
