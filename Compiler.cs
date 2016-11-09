using System;
using System.IO;
using System.Collections.Generic;

public sealed class Compiler
{
    public void DoTask(string[] args)
    {
        if(args.Length == 0)
        {
            return;
        }

        //TODO: arguments exception-free container

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
            m_interpreter.Run(m_generator.GetOutput());

            if(args[0] == "build")
            {

            }
            else if(args[0] == "run")
            {
                Interpretate();
            }
        }
#if (!DEBUG)
        catch(Exception e)
        {
            Console.WriteLine("Unhandled exception: '" + e.Message + "'. Aborting...");
        }
#endif
    }

    private void Interpretate()
    {
<<<<<<< HEAD
        //TODO builder
=======
        //TODO: builder
>>>>>>> temp
    }

    Lexer m_lexer = new Lexer();
    Parser m_parser = new Parser();
    Generator m_generator = new Generator();
    Interpreter m_interpreter = new Interpreter();
}