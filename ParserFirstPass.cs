using System;
using System.Collections.Generic;
using System.Linq;
using ALang;
using Lexemes = System.Collections.Generic.List<ALang.Lexeme>;


namespace ALang
{
    namespace Parser1To2Pass
    {
        public sealed class FunctionInfo
        {
            /// <summary>
            /// Reference to function information
            /// </summary>
            public LanguageFunction Info;

            /// <summary>
            /// Function body lexemes
            /// </summary>
            public Lexemes FuncLexemes;

            /// <summary>
            /// Function arguments' default values
            /// </summary>
            public List<Lexemes> ArgInitLexemes;

            public string ModuleName;
        }
    }

    public sealed partial class Parser
    {
        /// <summary>
        /// Main parse function. It is called from compiler
        /// </summary>
        /// <param name="modules"></param>
        public void Parse(List<LexemeModule> modules)
        {
            foreach (var module in modules)
            {
                var pos = 0;

                while (pos < module.Lexemes.Count)
                {
                    pos = FindElement(module, pos);
                }
            }
            ParseSecondPass();
        }

        /// <summary>
        /// It detects global scope elements and calls special handler
        /// </summary>
        /// <param name="module"></param>
        /// <param name="pos"></param>
        /// <returns>Position of a next lexeme</returns>
        private int FindElement(LexemeModule module, int pos)
        {
            if (module.Lexemes[pos].Code == Lexeme.CodeType.Reserved)
            {
                switch (module.Lexemes[pos].Source)
                {
                    case "using":
                        break;
                    case "function":
                        return ParseFunctionDeclaration(module, pos);
                    case "class":
                        break;
                    case "constexpr":
                        break;
                    default:
                        Compilation.WriteError("Unknown word: '" + module.Lexemes[pos] + "'.",
                            module.Lexemes[pos].Line);
                        break;
                }
            }

            return pos;
        }

        /// <summary>
        /// Parse function declaration and save it. (Function body is parsed at second pass)
        /// </summary>
        /// <param name="module"></param>
        /// <param name="pos"></param>
        /// <returns>Position of a next lexeme</returns>
        private int ParseFunctionDeclaration(LexemeModule module, int pos)
        {
            ++pos; //skip "function"

            Compilation.Assert(module.Lexemes[pos].Code == Lexeme.CodeType.Name,
                "Invalid function name: '" + module.Lexemes[pos].Source + "'.", module.Lexemes[pos].Line);

            var functionName = module.Lexemes[pos].Source;
            ++pos;

            Compilation.Assert(module.Lexemes[pos].Source == "(", "Did you forget the '(' ?", module.Lexemes[pos].Line);
            ++pos;

            var args = new List<LanguageFunction.FunctionArg>();

            var argsInitLexemes = new List<Lexemes>();

            if (module.Lexemes[pos].Source != ")")
            {
                while (true)
                {
                    string typeName;
                    string varName;
                    Lexemes initElements;

                    pos = ParseFuncVarDeclaration(module, pos, false, out typeName, out varName, out initElements);

                    args.Add(new LanguageFunction.FunctionArg
                    {
                        TypeInfo = m_symbols.GetTypeByName(typeName),
                        ArgName = varName,
                        DefaultVal = null /*Will be set in the 2nd pass*/
                    });
                    argsInitLexemes.Add(initElements);

                    if (module.Lexemes[pos].Source == ")")
                    {
                        break;
                    }
                    else if (module.Lexemes[pos].Source == ",")
                    {
                        ++pos;
                    }
                    else
                    {
                        Compilation.WriteError("Expected ',' or ')', but found '" + module.Lexemes[pos].Source + "'.",
                            module.Lexemes[pos].Line);
                    }
                }
            }

            ++pos; //skip ")"

            Compilation.Assert(module.Lexemes[pos].Source == "->", "Did you forget the '->' ?",
                module.Lexemes[pos].Line);
            ++pos;

            bool hasOneReturnVar = true;
            if (module.Lexemes[pos].Source == "(")
            {
                hasOneReturnVar = false;
                ++pos;
            }

            List<string> returnVars = new List<string>();
            do
            {
                Compilation.Assert(module.Lexemes[pos].Code == Lexeme.CodeType.Reserved ||
                                   module.Lexemes[pos].Code == Lexeme.CodeType.Name,
                    "'" + module.Lexemes[pos].Source + "' can't be a type", module.Lexemes[pos].Line);
                returnVars.Add(module.Lexemes[pos].Source);
                ++pos;

                if (hasOneReturnVar)
                    break;

                if (module.Lexemes[pos].Source == ")")
                {
                    ++pos;
                    break;
                }

                if (module.Lexemes[pos].Source != ",")
                {
                    Compilation.WriteError("Expected ',', but found '" + module.Lexemes[pos].Source + "'.",
                        module.Lexemes[pos].Line);
                }

                ++pos;
            } while (true);

            var funcInfo = new LanguageFunction
            {
                Name = functionName,
                Arguments = args,
                ReturnTypes = returnVars.Select(typeName =>
                        m_symbols.GetTypeByName(typeName))
                    .ToList()
            };
            bool ok = m_symbols.AddUserFunction(funcInfo);
            if (!ok)
            {
                Compilation.WriteError("Function " + funcInfo.Name + "' with arguments ["
                                       + funcInfo.Arguments.Select(arg => arg.TypeInfo.Name)
                                           .Aggregate((arg1, arg2) => arg1 + ", " + arg2)
                                       + "] already exists", module.Lexemes[pos].Line);
            }


            Lexemes funcLexemes;
            pos = ExtractBlock(module, pos, out funcLexemes);

            m_foundedFunctions.Add(new Parser1To2Pass.FunctionInfo
            {
                Info = funcInfo,
                FuncLexemes = funcLexemes,
                ArgInitLexemes = argsInitLexemes,
                ModuleName = module.FileName
            });

            return pos;
        }

        /// <summary>
        /// Parse variable declaration at function declaration
        /// </summary>
        /// <param name="module">Module</param>
        /// <param name="pos">Position of current parsed lexeme</param>
        /// <param name="assertOnUnknownType">Generate error if type isn't defined</param>
        /// <param name="typeName">Variable type</param>
        /// <param name="varName">Variable name</param>
        /// <param name="initElements">Lexemes that inialize this variable. The length can be zero if there no default value</param>
        /// <returns>Position of a next lexeme</returns>
        private int ParseFuncVarDeclaration(LexemeModule module, int pos, bool assertOnUnknownType,
            out string typeName, out string varName, out Lexemes initElements)
        {
            Compilation.Assert(m_symbols.IsTypeExist(module.Lexemes[pos].Source)
                               || (!assertOnUnknownType && module.Lexemes[pos].Code == Lexeme.CodeType.Name),
                "Type '" + module.Lexemes[pos] + "' does not exist", module.Lexemes[pos].Line);
            typeName = module.Lexemes[pos].Source;
            ++pos;

            Compilation.Assert(module.Lexemes[pos].Code == Lexeme.CodeType.Name,
                "Name '" + module.Lexemes[pos].Source + "' is not correct", module.Lexemes[pos].Line);
            varName = module.Lexemes[pos].Source;

            ++pos;

            initElements = new Lexemes();

            if (module.Lexemes[pos].Source == "=")
            {
                ++pos;
                int argScopeLevel = 0;
                while (true)
                {
                    if (module.Lexemes[pos].Source == "(")
                    {
                        ++argScopeLevel;
                    }
                    else if (module.Lexemes[pos].Source == ")")
                    {
                        --argScopeLevel;
                        if (argScopeLevel < 0)
                            break;
                    }
                    else if (module.Lexemes[pos].Source == ",")
                    {
                        if (argScopeLevel == 0)
                            break;
                    }

                    initElements.Add(module.Lexemes[pos]);
                    ++pos;
                }
            }

            return pos;
        }

        /// <summary>
        /// Extract function body in a list of lexemes
        /// </summary>
        /// <param name="module">Module</param>
        /// <param name="pos">Position of current parsed lexeme</param>
        /// <param name="blockLexemes">Function body lexemes</param>
        /// <returns>Position of a next lexeme</returns>
        private int ExtractBlock(LexemeModule module, int pos, out Lexemes blockLexemes)
        {
            Compilation.Assert(module.Lexemes[pos].Source == "{", "Did you forget '{' ?", module.Lexemes[pos].Line);

            int level = 0;
            ++pos;
            ++level;

            blockLexemes = new Lexemes();
            while (true)
            {
                if (pos >= module.Lexemes.Count)
                {
                    Compilation.WriteError("Did you forget '}' ?", module.Lexemes[pos - 1].Line);
                }

                if (module.Lexemes[pos].Source == "{")
                    ++level;
                else if (module.Lexemes[pos].Source == "}")
                {
                    --level;
                    if (level == 0)
                    {
                        ++pos;
                        break;
                    }
                }

                blockLexemes.Add(module.Lexemes[pos]);

                ++pos;
            }

            return pos;
        }

        /// <summary>
        /// Reference to all program elements (functions, types, priorities, convertions, etc)
        /// </summary>
        LanguageSymbols m_symbols = new LanguageSymbols();

        public static int CurrentLine;
    }
}