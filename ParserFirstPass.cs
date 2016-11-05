using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Lexems = System.Collections.Generic.List<Lexem>;

namespace Parser1To2Pass
{
    sealed public class FunctionInfo
    {
        public LanguageFunction Info;
        public Lexems FuncLexems;
        public List<Lexems> ArgInitLexems;
    }
}

public sealed partial class Parser
{
    public void Parse(Lexems lexems)
    {
        int pos = 0;

        while(pos < lexems.Count)
        {
            pos = FindElement(lexems, pos);
        }

        ParseSecondPass();
    }

    private int FindElement(Lexems lexems, int pos)
    {
        if(lexems[pos].codeType == Lexem.CodeType.Reserved)
        {
            switch(lexems[pos].source)
            {
            case "using":
                break;
            case "function":
                return ParseFunctionDeclaration(lexems, pos);
            case "class":
                break;
            case "constexpr":
                break;
            default:
                Compilation.WriteError("Unknown word: '" + lexems[pos] + "'.", lexems[pos].line);
                break;
            }
        }

        return pos;
    }
    private int ParseFunctionDeclaration(Lexems lexems, int pos)
    {
        ++pos;//skip "function"

        Compilation.Assert(lexems[pos].codeType == Lexem.CodeType.Name,
                           "Invalid function name: '" + lexems[pos].source + "'.", lexems[pos].line);

        string functionName = lexems[pos].source;
        ++pos;

        Compilation.Assert(lexems[pos].source == "(", "Did you forget the '(' ?", lexems[pos].line);
        ++pos;

        List<LanguageFunction.FunctionArg> args = new List<LanguageFunction.FunctionArg>();

        List<Lexems> argsInitLexems = new List<Lexems>();

        if(lexems[pos].source != ")")
        {
            string typeName;
            string varName;
            Lexems initElements;

            while(true)
            {
                pos = ParseFuncVarDeclaration(lexems, pos, false, out typeName, out varName, out initElements);

                args.Add(new LanguageFunction.FunctionArg{ TypeInfo = m_symbols.GetTypeByName(typeName),
                                                           ArgName = varName,
                                                           DefaultVal = null/*Will be set in the 2nd pass*/ });
                argsInitLexems.Add(initElements);

                if(lexems[pos].source == ")")
                    break;
                else if(lexems[pos].source == ",")
                {
                    ++pos;
                    continue;
                }
                else
                {
                    Compilation.WriteError("Expected ',' or ')', but found '" + lexems[pos].source + "'.", lexems[pos].line);
                }
            }
        }

        ++pos;//skip ")"

        Compilation.Assert(lexems[pos].source == "->", "Did you forget the '->' ?", lexems[pos].line);
        ++pos;

        bool hasOneReturnVar = true;
        if(lexems[pos].source == "(")
        {
            hasOneReturnVar = false;
            ++pos;
        }   

        List<string> returnVars = new List<string>();
        do  
        {
            Compilation.Assert(lexems[pos].codeType == Lexem.CodeType.Reserved ||
                               lexems[pos].codeType == Lexem.CodeType.Name,
                               "'" + lexems[pos].source + "' can't be a type", lexems[pos].line);
            returnVars.Add(lexems[pos].source);
            ++pos;
            
            if(hasOneReturnVar)
                break;

            if(lexems[pos].source == ")")
            {
                ++pos;
                break;
            }
            
            if(lexems[pos].source != ",")
            {
                Compilation.WriteError("Expected ',', but found '" + lexems[pos].source + "'.", lexems[pos].line);
            }

            ++pos;
        }
        while(true);

        var funcInfo = new LanguageFunction{ Name = functionName, Arguments = args, 
                                             ReturnTypes = returnVars.Select(typeName => 
                                                                                m_symbols.GetTypeByName(typeName))
                                                                     .ToList() };
        bool ok = m_symbols.AddUserFunction(funcInfo);
        if(!ok)
        {
            Compilation.WriteError("Function " + funcInfo.Name + "' with arguments [" 
                                   + funcInfo.Arguments.Select(arg => arg.TypeInfo.Name)
                                                       .Aggregate((arg1, arg2) => arg1 + ", " + arg2)
                                   + "] already exists", lexems[pos].line);
        }


        Lexems funcLexems;
        pos = ExtractBlock(lexems, pos, out funcLexems);

        m_foundedFunctions.Add(new Parser1To2Pass.FunctionInfo
                                    { Info = funcInfo, FuncLexems = funcLexems, ArgInitLexems = argsInitLexems });

        return pos;
    }

    private int ParseFuncVarDeclaration(Lexems lexems, int pos, bool assertOnUnknownType,
                                    out string typeName, out string varName, out Lexems initElements)
    {
        Compilation.Assert(m_symbols.IsTypeExist(lexems[pos].source)
                           || (!assertOnUnknownType && lexems[pos].codeType == Lexem.CodeType.Name),
                           "Type '" + lexems[pos] + "' does not exist", lexems[pos].line);
        typeName = lexems[pos].source;
        ++pos;

        Compilation.Assert(lexems[pos].codeType == Lexem.CodeType.Name,
                            "Name '" + lexems[pos].source + "' is not correct", lexems[pos].line);
        varName = lexems[pos].source;

        ++pos;

        initElements = new Lexems();

        if(lexems[pos].source == "=")
        {
            ++pos;
            int argScopeLevel = 0;
            while(true)
            {
                if(lexems[pos].source == "(")
                {
                    ++argScopeLevel;
                }
                else if(lexems[pos].source == ")")
                {
                    --argScopeLevel;
                    if(argScopeLevel < 0)
                        break;
                }
                else if(lexems[pos].source == ",")
                {
                    if(argScopeLevel == 0)
                        break;
                }

                initElements.Add(lexems[pos]);
                ++pos;
            }
        }

        return pos;
    }

    private int ExtractBlock(Lexems lexems, int pos, out Lexems blockLexems)
    {
        Compilation.Assert(lexems[pos].source == "{", "Did you forget '{' ?", lexems[pos].line);

        int level = 0;
        ++pos;
        ++level;

        blockLexems = new Lexems();
        while(true)
        {
            if(pos >= lexems.Count)
            {
                Compilation.WriteError("Did you forget '}' ?", lexems[pos-1].line);
            }

            if(lexems[pos].source == "{")
                ++level;
            else if(lexems[pos].source == "}")
            {
                --level;
                if(level == 0)
                {
                    ++pos;
                    break;
                }
            }

            blockLexems.Add(lexems[pos]);

            ++pos;
        }

        return pos;
    }

    LanguageSymbols m_symbols = new LanguageSymbols();
    public static int CurrentLine;
}