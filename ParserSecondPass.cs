using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Lexemes = System.Collections.Generic.List<Lexeme>;

/// <summary>
/// It'll be passed to generator
/// </summary>
sealed public class ParserOutput
{
    /// <summary>
    /// List of functions' trees
    /// </summary>
    public List<FunctionElement> Functions = new List<FunctionElement>();
}

/// <summary>
/// Flags to change an expression parsing 
/// </summary>
[Flags]
public enum ExpressionFlags
{
    /// <summary>
    /// Default, no flags
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Default keyword, compiler detects skipped value
    /// </summary>
    AllowAutoDefault = 1,
    
    /// <summary>
    /// Default(Type), use default type value
    /// </summary>
    AllowDefaultValue = 2,
    
    /// <summary>
    /// Can expression use only values without operators 
    /// </summary>
    OperationRequired = 4,

    /// <summary>
    /// Expression can be empty
    /// </summary>
    AllowEmpty = 8
}

/// <summary>
/// Converts lexemes into tree
/// </summary>
public sealed partial class Parser
{
    /// <summary>
    /// Returns parse result
    /// </summary>
    /// <returns></returns>
    public ParserOutput GetParserOutput()
    {
        return m_parserOutput;
    }

    /// <summary>
    /// Parse prepared data. It's called from first pass
    /// </summary>
    private void ParseSecondPass()
    {
        var builtInFuncs = BuiltInFunctions.GetFunctions();
        m_parserOutput.Functions.AddRange(builtInFuncs);

        //Parse all declarations
        foreach(var funcInfo in m_foundedFunctions)
        {
            ParseFunctionDefaultVals(funcInfo);
        }
        
        //Parse all definitions
        foreach(var funcInfo in m_foundedFunctions)
        {
            ParseFunction(funcInfo);
        }
    }

    /// <summary>
    /// Parse function default arguments and save it before body parsing
    /// </summary>
    /// <param name="funcInfo">Parsed function</param>
    private void ParseFunctionDefaultVals(Parser1To2Pass.FunctionInfo funcInfo)
    {
        ValueElement valElement;

        for(int i = 0, end = funcInfo.ArgInitLexemes.Count; i < end; ++i)
        {
            if(funcInfo.ArgInitLexemes[i].Count != 0)
            {
                ParseExpression(funcInfo.ArgInitLexemes[i], 0, -1, ExpressionFlags.AllowDefaultValue, out valElement);
            }
            else
            {
                valElement = null;
            }

            funcInfo.Info.Arguments[i].DefaultVal = valElement;
        }
        m_symbols.UpdateFunction(funcInfo.Info);
    }

    /// <summary>
    /// Parse function body
    /// </summary>
    /// <param name="funcInfo">Function to parse</param>
    private void ParseFunction(Parser1To2Pass.FunctionInfo funcInfo)
    {
        FunctionElement funcElem = new FunctionElement();
        m_currentFunction = funcElem;
        
        funcElem.Info = funcInfo.Info;

        StatementListElement statements = new StatementListElement();
        TreeElement element;

        m_currentBlock.Push(statements);

        VarDeclarationElement declElem;
        foreach(var arg in funcInfo.Info.Arguments)
        {
            declElem = new VarDeclarationElement{ VarType = arg.TypeInfo.Name, 
                                                  VarName = arg.ArgName,
                                                  IsInitGeneratedBefore = true };
            declElem.SetParent(statements);
            funcElem.ArgumentsVars.Add(declElem);
            //funcElem.LocalVars.Add(declElem);//TODO: remove it?
        }

        if(funcInfo.FuncLexemes.Count > 0)
        {
            for(int i = 0, end = funcInfo.FuncLexemes.Count;;)
            {
                i = ParseStatement(funcInfo.FuncLexemes, i, out element);
                if(element != null)//if null => ignore (block element)
                {
                    m_currentBlock.Peek().AddChild(element);
                }

                if(i >= end)
                    break;
            }   
        }

        funcElem.AddChild(statements);

        m_parserOutput.Functions.Add(funcElem);

        m_currentBlock.Pop();
    }

    /// <summary>
    /// Find and build statement. If there are no statements it will parse expression.
    /// </summary>
    /// <param name="lexemes">List of lexemes</param>
    /// <param name="pos">Position of current parsed lexeme</param>
    /// <param name="elem">Statement builded on lexemes. null if it's a block</param>
    /// <returns>Position of a next Lexeme</returns>
    private int ParseStatement(Lexemes lexemes, int pos, out TreeElement elem)
    {
        if(m_symbols.IsTypeExist(lexemes[pos].source))
        {
            return ParseVarDeclaration(lexemes, pos, out elem);
        }
        else if(lexemes[pos].source == "{")
        {
            return ParseBlock(lexemes, pos, out elem);
        }
        else if(lexemes[pos].source == "}")
        {
            Compilation.Assert(m_currentBlock.Count > 0, "Unexpected '}'. Did you forget '{' ?", pos);

            m_currentBlock.Pop();
            elem = null;
            return ++pos;
        }
        else if(lexemes[pos].source == "print")//TODO: remove it
        {
            ++pos;

            PrintCurrentValElement printElem = new PrintCurrentValElement();
            string varName = lexemes[pos].source;
            printElem.VarGet = new VarGetSetValElement{ VarName = varName };

            ++pos;
            
            elem = printElem;

            return pos;
        }
        else
        {
            ValueElement expr;  
            pos = ParseExpression(lexemes, pos, -1, ExpressionFlags.OperationRequired | ExpressionFlags.AllowDefaultValue,
                                  out expr);
            elem = expr;

            ++pos;//skip ';'

            return pos;
        }
    }

    /// <summary>
    /// Add new block
    /// </summary>
    /// <param name="lexemes">List of lexemes</param>
    /// <param name="pos">Position of current parsed lexeme</param>
    /// <param name="elem">Statement builded on lexemes. Always null</param>
    /// <returns>Position of a next lexeme</returns>
    private int ParseBlock(Lexemes lexemes, int pos, out TreeElement elem)
    {
        StatementListElement block = new StatementListElement();

        m_currentBlock.Peek().AddChild(block);
        m_currentBlock.Push(block);

        ++pos;//skip '{'

        elem = null;

        return pos;
    }

    /// <summary>
    /// Builds variable declaration element (with optional initialization)
    /// </summary>
    /// <param name="lexemes">List of lexemes</param>
    /// <param name="pos">Position of current parsed lexeme</param>
    /// <param name="elem">Element builded on lexemes</param>
    /// <returns>Position of a next lexeme</returns>
    private int ParseVarDeclaration(Lexemes lexemes, int pos, out TreeElement elem)
    {
        MultipleVarDeclarationElement multipleDecl = new MultipleVarDeclarationElement();

        VarDeclarationElement varDecl = new VarDeclarationElement();

        bool expectVar = true;
        bool hasInitialization = false;
        bool breakCycle = false;

        for(;;)
        {
            switch(lexemes[pos].source)
            {
                case ",":
                {
                    Compilation.Assert(!expectVar, "Expected variable declaration, not comma", lexemes[pos].line);

                    varDecl = new VarDeclarationElement();
                    expectVar = true;

                    ++pos;
                    continue;
                }
                case "=":
                    hasInitialization = true;
                    breakCycle = true;
                    ++pos;
                    break;
                case ";":
                    hasInitialization = false;
                    breakCycle = true;
                    ++pos;
                    break;
            }

            if(breakCycle)
            {
                break;
            }

            varDecl.VarType = lexemes[pos].source;
            Compilation.Assert(m_symbols.IsTypeExist(varDecl.VarType), "Unknown type '" + varDecl.VarType + "'", lexemes[pos].line);
            
            ++pos;
            varDecl.VarName = lexemes[pos].source;
            Compilation.Assert(lexemes[pos].codeType == Lexeme.CodeType.Name,
                               "Invalid variable name '" + varDecl.VarName + "'", lexemes[pos].line);

            multipleDecl.AddVar(varDecl);
            expectVar = false;
            
            ++pos;
        }

        if(!hasInitialization)
        {
            foreach(var variable in multipleDecl.GetVars()) 
            {
                variable.InitVal = m_symbols.GetDefaultVal(variable.VarType);
            }
        }
        else
        {
            ValueElement initElements;
            pos = ParseExpression(lexemes, pos, -1, ExpressionFlags.AllowDefaultValue | ExpressionFlags.AllowAutoDefault,
                                  out initElements); 
            //TODO: auto default

            Compilation.Assert(multipleDecl.GetVars().Count == initElements.ValCount, 
                               "Each variable associated with only one expression (variables: " 
                               + multipleDecl.GetVars().Count +
                               ", initalization expressions: " + initElements.ChildrenCount(), lexemes[pos-1].line);

            for(int i = 0, end = multipleDecl.GetVars().Count; i < end; ++i)
            {
                multipleDecl.GetVar(i).InitVal = (ValueElement)initElements.Child(i);
            }

            Compilation.Assert(lexemes[pos].source == ";", "Did you forget ';' ?", lexemes[pos].line);
            ++pos;//skip ';'
        }

        foreach(var decl in multipleDecl.GetVars())
        {
            m_currentFunction.LocalVars.Add(decl);
        }

        elem = multipleDecl;
        m_currentBlock.Peek().LocalVariables.AddRange(multipleDecl.GetVars());

        return pos;
    }

    /// <summary>
    /// Build expression element
    /// </summary>
    /// <param name="lexemes">List of lexemes</param>
    /// <param name="pos">Position of current parsed lexeme</param>
    /// <param name="endPos">Position at which function breaks. Pass -1 to parse until the end</param>
    /// <param name="flags">Control flags. ExpressionFlags.None to ignore all flags</param>
    /// <param name="elem"></param>
    /// <returns>Position of a next lexeme</returns>
    private int ParseExpression(Lexemes lexemes, int pos, int endPos,
                                ExpressionFlags flags, 
                                out ValueElement elem)
    {    
        Lexeme currentLexeme;

        OperationElement lastOperation = null;
        OperationElement rootOperation = null;
        ValueElement lastValElement = null;

        bool breakCycle = false;

        if(endPos == -1)
        {
            endPos = lexemes.Count;
        }

        for(int end = endPos; !breakCycle && pos < end;)
        {
            currentLexeme = lexemes[pos];

            switch(currentLexeme.source)
            {
                case ")": 
                case ";":
                case "{":
                case "}":
                       breakCycle = true;
                       continue;
                case ",":
                    Compilation.WriteError("Unexpected symbol: ','." +
                                           "Did you forget '(' ?\nExample: '( SYMBOLS, SYMBOLS )'.",
                                            lexemes[pos].line);
                    continue;
                case "+":
                case "-":
                case "*":
                case "/":
                case "^":
                case "=":
                    {
                        if(lastValElement == null && lastOperation == null)
                        {
                            if(lexemes[pos].source == "-")
                            {
                                rootOperation = lastOperation = new UnaryMinusElement();
                                ++pos;
                                continue;
                            }
                            else
                            {
                                Compilation.WriteError("Expected value or expression before '" 
                                                       + lexemes[pos].source + "'", lexemes[pos].line);
                            }
                        }
                        
                        OperationElement operation = null;
                        switch(lexemes[pos].source)
                        {
                            case "+":
                                operation = new BinaryPlusElement();
                                break;
                            case "-":
                                operation = new BinaryMinusElement();
                                break;
                            case "*":
                                operation = new BinaryMultiplicationElement();
                                break;
                            case "/":
                                operation = new BinaryDivisionElement();
                                break;
                            case "^":
                                operation = new BinaryExponentiationElement();
                                break;
                            case "=":
                                operation = new CopyElement();
                                break;
                            default:
                                Compilation.WriteCritical("BUG: Unknown operator passed through the switch");
                                break;
                        }

                        if(lastOperation == null) 
                        {
                            operation.SetChild(0, lastValElement);
                            lastValElement = null;
                            rootOperation = lastOperation = operation;
                        }
                        else
                        {
                            InsertOperationInTree(ref lastOperation, ref operation, ref rootOperation);
                        }

                        lastOperation = operation;
                        
                    }
                    ++pos;
                    break;
                default:
                    pos = ParseExpressionValue(lexemes, pos, flags | ExpressionFlags.AllowDefaultValue 
                                                                  & (~ExpressionFlags.OperationRequired),
                                               out lastValElement);
                    if(lastValElement == null)
                    {
                        Compilation.WriteError("Unknown symbol: '" + currentLexeme.source + "'.", currentLexeme.line);
                    }
                    else
                    {
                        if(lastOperation != null)
                        {
                            InsertValInTree(lastOperation, lastValElement, true);
                            lastValElement = null;
                        } 
                    }
                    break;                    
            }
        }
 
        if(rootOperation == null)
        {
            if(flags.HasFlag(ExpressionFlags.OperationRequired) && !(lastValElement is FunctionCallElement))
            {
                Compilation.WriteError("Expression must contain at least one operator", lexemes[pos].line);
            }
            if(lastValElement == null)
            {
                Compilation.WriteError("No operation or value in expression", lexemes[pos].line); 
            }
            elem = lastValElement;
        }
        else
        {
            elem = rootOperation;
        }

        return pos;
    }

    /// <summary>
    /// Build expression value. It's called from ParseExpression
    /// </summary>
    /// <param name="lexemes">List of lexemes</param>
    /// <param name="pos">Position of current parsed lexeme</param>
    /// <param name="flags">Control flags. ExpressionFlags.None to ignore all flags</param>
    /// <param name="elem">Builded value element. null if it's not a value</param>
    /// <returns>Position of a next lexeme</returns>
    private int ParseExpressionValue(Lexemes lexemes, int pos, ExpressionFlags flags, out ValueElement elem) 
    {
        MultipleValElement multipleVals = new MultipleValElement();
        
        if(lexemes[pos].source == "(")
        {
            List<ValueElement> values;

            pos = ExtractSeparatedExpressions(lexemes, pos, flags, out values);

            if(pos >= (lexemes.Count-1))
            {
                Compilation.WriteError("Unexpected end of file. Did you forget ')' and ';' ?", -1);
            }

            foreach(var i in values)
            {
                multipleVals.AddValue(i);
            }
        }//"("
        else if(lexemes[pos].codeType == Lexeme.CodeType.Number)
        {
            var val = new ConstValElement{ Type = m_symbols.GetTypeOfConstVal(lexemes[pos].source),
                                            Value = lexemes[pos].source };
            ++pos;
            elem = val;
            return pos;
        }
        else if(m_currentFunction.HasVariable(lexemes[pos].source) && lexemes[pos+1].source != "(")
        {
            var val = new VarGetSetValElement{ VarName = lexemes[pos].source };

            ++pos;
            elem = val;
            return pos;
        }
        else if(m_symbols.HasFunctionWithName(lexemes[pos].source))
        {
            FunctionCallElement callElement = new FunctionCallElement();

            string funcName = lexemes[pos].source;
            
            ++pos;
            List<ValueElement> values;

            if(lexemes[pos+1].source == ")")//fucntion without arguments
            {   
                values = new List<ValueElement>();
                pos += 2;//'(' ')'
            }
            else
            {
                pos = ExtractSeparatedExpressions(lexemes, pos, 
                                                  flags | ExpressionFlags.AllowAutoDefault,
                                                  out values);
            }
            if(values.Count == 1 && values[0].ValCount > 1)
            {
                values = values[0].Children().Select(child => (ValueElement)child).ToList();
            }

            callElement.FunctionInfo = m_symbols.GetFunction(funcName, values.Select(val => val.Result.ResultTypes[0].ResultType).ToList());

            for(int i = 0, end = values.Count; i < end; ++i)
            {
                if(values[i] == null)
                {
                    values[i] = callElement.FunctionInfo.Arguments[i].DefaultVal;
                }
            }

            callElement.CallArguments = values;

            elem = callElement;

            return pos;
        }
        else if(lexemes[pos].source == "default")
        {
            ++pos;

            if(lexemes[pos].source == "(")
            {
                if(!flags.HasFlag(ExpressionFlags.AllowAutoDefault))
                {
                    Compilation.Assert(flags.HasFlag(ExpressionFlags.AllowDefaultValue), 
                                       "Default type value keyword is used but it isn't allowed here.", lexemes[pos].line);
                }

                ++pos;//skip '('

                string typeName = lexemes[pos].source;//TODO: namespaces

                ConstValElement constVal = m_symbols.GetDefaultVal(typeName);

                elem = new ConstValElement{ Type = constVal.Type, Value = constVal.Value };

                ++pos;
                ++pos;//skip ')'
            }
            else
            {
                Compilation.Assert(flags.HasFlag(ExpressionFlags.AllowAutoDefault), 
                                   "Auto default keyword is used but it isn't allowed here.", lexemes[pos].line);
                elem = null;
            }

            return pos;
        }
        else//Not a value
        {
            elem = null;
            return pos; 
        }

        elem = multipleVals;
        return pos;
    }

    /// <summary>
    /// Takes value and insert it in a operation
    /// </summary>
    /// <param name="operation">Used operation</param>
    /// <param name="value">Used value</param>
    /// <param name="right">If true a value will be inserted as right operand, false - as left</param>
    private void InsertValInTree(OperationElement operation, ValueElement value, bool right = true)
    {
        if(right)
        {
            if(operation.HasRightOperand)
            {
                if(operation.Child(1) == null)
                {
                    operation.SetChild(1, value);
                }
                else
                {
                    Compilation.WriteError("Operation '" + operation.OperationName +
                                           "' already has right operand", value.Line);
                }
            }
            else
            {
                Compilation.WriteError("Operation '" + operation.OperationName + 
                                       "' hasn't right operand", value.Line);
            }
        }
        else
        {
            if(operation.HasLeftOperand)
            {
                if(operation.Child(0) == null)
                {
                    operation.SetChild(0, value);
                }
                else
                {
                    Compilation.WriteError("Operation '" + operation.OperationName + 
                                           "' already has left operand", value.Line);
                }
            }
            else
            {
                Compilation.WriteError("Operation '" + operation.OperationName + 
                                       "' hasn't left operand", value.Line);
            }
        }
    }

    /// <summary>
    /// Insert operation in a operation tree using priorities
    /// </summary>
    /// <param name="lastOperation">Previous builded operation</param>
    /// <param name="operation">Current operation</param>
    /// <param name="rootOperation">Root operation of a tree</param>
    private void InsertOperationInTree<T>(ref OperationElement lastOperation, 
                                          ref T operation, 
                                          ref OperationElement rootOperation)
                                where T : OperationElement
    {
        if(operation.Priority > lastOperation.Priority ||
          (operation.OperationName == lastOperation.OperationName && 
            !OperationPriority.IsCompareToThisAsLessPriority[operation.OperationName]))
        {
            operation.SetChild(0, lastOperation.Child(1));

            lastOperation.SetChild(1, operation);
        }
        else
        {
            if(lastOperation.Parent() != null)
            {
                OperationElement parentOperation = (OperationElement)lastOperation.Parent();
                InsertOperationInTree(ref parentOperation, ref operation, ref rootOperation);
            }
            else
            {
                operation.SetChild(0, lastOperation);
                rootOperation = lastOperation = operation;
            }
        }
    }

    /// <summary>
    /// Build list of expression separated by comma
    /// </summary>
    /// <param name="lexemes">List of lexemes</param>
    /// <param name="pos">Position of current parsed lexeme</param>
    /// <param name="flags">Control flags. ExpressionFlags.None to ignore all flags</param>
    /// <param name="elements">Builded expressions</param>
    /// <returns>Position of a next lexeme</returns>
    private int ExtractSeparatedExpressions(Lexemes lexemes, int pos, ExpressionFlags flags,
                                            out List<ValueElement> elements)
    {
        elements = new List<ValueElement>();
        ValueElement val;
        int currentLevel = 1;

        ++pos;//skip '('

        for(int i = pos; currentLevel != 0 && i < lexemes.Count;)
        {
            switch(lexemes[i].source)
            {
                case ";":
                    Compilation.WriteError("Unexpected symbol ';'. Did you forget ')' ?", lexemes[i].line);
                    break;
                case "(":
                    ++currentLevel;
                    ++i;
                    continue;
                case ")":
                    --currentLevel;
                    if(currentLevel == 0)
                    {
                        pos = i = ParseExpression(lexemes, pos, i, flags & (~ExpressionFlags.OperationRequired)
                                                                | ExpressionFlags.AllowDefaultValue, out val) + 1;
                        elements.Add(val);
                    }
                    else
                    {
                        ++i;
                    }
                    continue;
                case ",":
                    if(currentLevel == 1)
                    {
                        pos = i = ParseExpression(lexemes, pos, i, flags & (~ExpressionFlags.OperationRequired)
                                                                | ExpressionFlags.AllowDefaultValue, out val) + 1;
                        elements.Add(val);
                    }
                    else
                    {
                        ++i;
                    }
                    break;
                default:
                    ++i;
                    break;
            }
        }
        return pos;
    }

    /// <summary>
    /// Reference to processing function
    /// </summary>
    private FunctionElement m_currentFunction;

    /// <summary>
    /// List of processing blocks
    /// </summary>
    private Stack<StatementListElement> m_currentBlock = new Stack<StatementListElement>();

    /// <summary>
    /// It's saved at first pass
    /// </summary>
    private List<Parser1To2Pass.FunctionInfo> m_foundedFunctions = new List<Parser1To2Pass.FunctionInfo>();

    /// <summary>
    /// It store program tree that is used in a generator
    /// </summary>
    private ParserOutput m_parserOutput = new ParserOutput();
}