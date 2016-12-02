using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

public class SourceFileInfo
{
    public string SourceCode;
    public string FileName;
}

/// <summary>
/// It's a compiler class
/// </summary>
public sealed class Compiler
{
    public static Compiler Instance
    {
        get
        {
            if(m_instance == null)
            {
                m_instance = new Compiler();
            }
            return m_instance;
        }
    }

    public CompilationArguments Arguments
    {
        get
        {
            return m_arguments;
        }
    }
    public Parser Parser
    {
        get
        {
            return m_parser;
        }
    }

    /// <summary>
    /// Execute operations using arguments
    /// </summary>
    /// <param name="args">Compiler arguments</param>
    public void DoTask(string[] args)
    {
        if(!m_arguments.Parse(args))
        {
            return;
        }

#if (!DEBUG)
        try
        {
#endif
            if(!Arguments.HasCommand("source", false))
            {
                Console.WriteLine("No sources. Aborting compilation.");
                return;
            }

            var sources = ReadSourceFiles();

            m_lexer.Convert(sources);
            m_parser.Parse(m_lexer.GetOutput());
            m_generator.Generate(m_parser.GetParserOutput());

            m_saver.Program = m_generator.GetOutput();
            m_saver.Save("program.alang");
        
#if (!DEBUG)
        }
        catch(CompileException e)
        {
            Console.WriteLine("Compilation error. Abort building.");
        }
        catch
        {
            Console.WriteLine("Unhandled exception. Aborting...");
            throw;
        }
#endif
    }

    private List<SourceFileInfo> ReadSourceFiles()
    {
        List<SourceFileInfo> files = new List<SourceFileInfo>();

        var inputFiles = Arguments.GetArgumentsFor("source");

        foreach(var inputFile in inputFiles)
        {
            if(File.Exists(inputFile))
            {
                files.Add(new SourceFileInfo{ FileName = inputFile, SourceCode = File.ReadAllText(inputFile) });
            }
            else
            {
                Compilation.WriteError(string.Format("File '{0}' doesn't exist."), -1);
                return null;
            }
        }

        return files;
    }

    private Compiler() {}
    
    private static Compiler m_instance;
    
    CompilationArguments m_arguments = new CompilationArguments();
    Lexer m_lexer = new Lexer();
    Parser m_parser = new Parser();
    Generator m_generator = new Generator();
    ProgramToFileSaver m_saver = new ProgramToFileSaver();
}