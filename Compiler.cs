using System;
using System.IO;
using System.Diagnostics;

/// <summary>
/// It's a compiler class
/// </summary>
public sealed class Compiler
{
    /// <summary>
    /// Execute operations using arguments
    /// </summary>
    /// <param name="args">Compiler arguments</param>
    //TODO: arguments documentation
    public void DoTask(string[] args)
    {
        if(args.Length == 0)
        {
            return;
        }

        string source;  

        if(File.Exists(args[1]))
        {
            source = File.ReadAllText(args[1]);
        }
        else
        {
            Console.WriteLine("File doesn't exist: " + args[1] + "");
            return;
        }

#if (!DEBUG)
        try
#endif
        {
            m_lexer.Convert(source);
            m_parser.Parse(m_lexer.GetLexems());
            m_generator.Generate(m_parser.GetParserOutput());

            m_saver.Program = m_generator.GetOutput();
            m_saver.Save("program.alang");
        }
#if (!DEBUG)
        catch(Exception e)
        {
            Console.WriteLine("Unhandled exception: '" + e.Message + "'. Aborting...");
        }
#endif
    }

    /// <summary>
    /// Execute source after compilation
    /// </summary>
    private void Interpretate()
    {
        //TODO: builder
    }
    
    Lexer m_lexer = new Lexer();
    Parser m_parser = new Parser();
    Generator m_generator = new Generator();
    ProgramToFileSaver m_saver = new ProgramToFileSaver();
}