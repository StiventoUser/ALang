using System;
using System.IO;

namespace ALang
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var compiler = Compiler.Instance;
            compiler.DoTask(args);
        }
    }
}