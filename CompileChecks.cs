using System;

namespace ALang
{
    public class CompilationException : Exception
    {
    }

    public static class Compilation
    {
        public static void Abort()
        {
            Console.WriteLine("Aborting...");
            throw new CompilationException();
        }

        public static void Assert(bool condition, string text, int line)
        {
            if (!condition)
            {
                WriteError(text, line);
                Abort();
            }
        }

        public static void WriteInfo(string text, int line)
        {
            if (line == -1)
                line = ALang.Parser.CurrentLine;

            Console.WriteLine("[Line: " + line + "]Build information: " + text);
        }

        public static void WriteWarning(string text, int line)
        {
            if (line == -1)
                line = ALang.Parser.CurrentLine;

            Console.WriteLine("[Line: " + line + "]Warning: " + text);
        }

        public static void WriteError(string text, int line)
        {
            if (line == -1)
                line = ALang.Parser.CurrentLine;

            Console.WriteLine("[Line: " + line + "]Error: " + text);
            Abort();
        }

        public static void WriteCritical(string text)
        {
            Console.WriteLine("!!!Critical error!!!: " + text);
            Abort();
        }
    }
}