using System;
 
public class CompileException : Exception
{
}

public static class Compilation
{
    public static void Abort()
    {
        Console.WriteLine("Aborting...");
        throw new CompileException();
    }
    public static void Assert(bool condition, string text, int line)
    {
        if(!condition)
        {
            WriteError(text, line);
            Compilation.Abort();
        }
    }
    public static void WriteInfo(string text, int line)
    {
        if(line == -1)
            line = Parser.CurrentLine;

        Console.WriteLine("[Line: " + line + "]Compile info: " + text);
    }
    public static void WriteWarning(string text, int line)
    {
        if(line == -1)
            line = Parser.CurrentLine;
        
        Console.WriteLine("[Line: " + line + "]Compile warning: " + text);
    }
    public static void WriteError(string text, int line)
    {
        if(line == -1)
            line = Parser.CurrentLine;

        Console.WriteLine("[Line: " + line + "]Compile error: " + text);
        Compilation.Abort();
    }
    public static void WriteCritical(string text)
    {
        Console.WriteLine("!!!Critical error!!!: " + text);
        Compilation.Abort();
    }
}