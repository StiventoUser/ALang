using System;

namespace ALang
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
