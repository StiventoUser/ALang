using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CompilationArguments
{
    public bool HasCommand(string command, bool checkRequired = true)
    {
        bool hasCommand = m_result.Exists(r => r.FullName == command);
        if(!hasCommand)
        {
            var defaultInfo = m_commands.Find(c => c.FullName == command);

            if(defaultInfo == null)
            {
                Console.WriteLine(string.Format("BUG: Unknown command {0}", command));
                throw new InvalidOperationException("Unknown command");
            }

            if(defaultInfo.Required)
            {
                Console.WriteLine(string.Format("Command {0} is expected" + 
                                                (!string.IsNullOrEmpty(defaultInfo.ArgsInfo) 
                                                ? (" with arguments: " + defaultInfo.ArgsInfo) : ""), command));
                if(checkRequired)
                {
                    throw new CompileException(); 
                }
            }

            return false;
        }

        return true;
    }
    public string[] GetArgumentsFor(string command)
    {
        var resultInfo = m_result.Find(r => r.FullName == command);
        if(resultInfo == null)
        {
            var defaultInfo = m_commands.Find(c => c.FullName == command);

            if(defaultInfo == null)
            {
                Console.WriteLine(string.Format("BUG: Unknown command {0}", command));
                throw new InvalidOperationException("Unknown command");
            }

            if(defaultInfo.Required)
            {
                Console.WriteLine(string.Format("Command {0} is expected" + 
                                                (!string.IsNullOrEmpty(defaultInfo.ArgsInfo) 
                                                ? (" with arguments: " + defaultInfo.ArgsInfo) : ""), command));
                throw new CompileException();
            }

            return defaultInfo.DefaultArgs ?? new string[0];
        }

        return resultInfo.Arguments;
    }
    public bool Parse(string[] args)
    {
        string commandName;
        CommandInfo commandInfo;
        for(int i = 0; i < args.Length;)
        {
            if(args[i].StartsWith("--"))
            {
                commandName = args[i].Substring(2);

                commandInfo = m_commands.Find(c => c.FullName == commandName);
                if(commandInfo == null)
                {
                    Console.WriteLine(string.Format("Unknown command '{0}'.\nUse {1} --help to see all commands",
                                                    commandName, System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName));
                    return false;
                }
                if(!ParseCommand(commandInfo, args, ref i))
                {
                    return false;
                }
            }
            else if(args[i][0] == '-')
            {
                foreach(var arg in args[i].Substring(1))
                {
                    commandInfo = m_commands.Find(c => c.ShortName == arg);
                    if(commandInfo == null)
                    {
                        Console.WriteLine(string.Format("Unknown command '{0}'.\nUse {1} --help to see all commands",
                                                        arg, System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName));
                        return false;
                    }
                    if(!ParseCommand(commandInfo, args, ref i))
                    {
                        return false;
                    }
                }
            }
            else
            {
                Console.WriteLine(string.Format("Not a command '{0}'.\nUse {1} --help to see all commands",
                                                    args[i], System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName));
                return false;
            }
        }

        return true;
    }

    private bool ParseCommand(CommandInfo command, string[] args, ref int i)
    {
        CommandResult result = new CommandResult{ FullName = command.FullName }; 
        List<string> commandArgs = new List<string>();

        if(command.ArgCount == 0)
        {
            m_result.Add(result);
            return true;
        }
        for(int a = i + 1, n = command.ArgCount; a < args.Length; ++a)
        {
            if(args[a][0] == '-')
            {
                i = a;

                if(n != -1 && n > 0)
                {
                    Console.WriteLine(string.Format("Did you forget arguments for {0}: {1} ?",
                                                    command.FullName, command.ArgsInfo));
                    return false;
                }

                result.Arguments = commandArgs.ToArray();
                m_result.Add(result);
                return true;
            }

            if(n != -1)
            {
                if(n == 0)
                {
                    Console.WriteLine(string.Format("Did you forget arguments for {0}: {1} ?",
                                                    command.FullName, command.ArgsInfo));
                    return false;
                }
                --n;
            }
            commandArgs.Add(args[a]);
        }

        i = args.Length;
        result.Arguments = commandArgs.ToArray();
        m_result.Add(result);
        return true;
    }

    private class CommandInfo
    {
        public string FullName;
        public char ShortName;

        public bool Required;
        
        public int ArgCount;

        public string[] DefaultArgs = null;
        public string ArgsInfo;
    }
    private class CommandResult
    {
        public string FullName;
        public string[] Arguments = null;
    }

    private List<CommandInfo> m_commands = new List<CommandInfo>()
    {
        new CommandInfo{ FullName = "help", ArgCount = 0 },
        new CommandInfo{ FullName = "source", ShortName = 's', Required = true,
                         ArgCount = -1, ArgsInfo = "file1.alangs path/to/file/file2 file3 ..." },
        new CommandInfo{ FullName = "output", ShortName = 'o', Required = true,
                         ArgCount = 1, ArgsInfo = "path/to/file/file.alang" }
    };
    private List<CommandResult> m_result = new List<CommandResult>();
}