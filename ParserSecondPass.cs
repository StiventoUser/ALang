using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Lexems = System.Collections.Generic.List<Lexem>;

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
    OperationRequired = 4
}

/// <summary>
/// Converts lexems into tree
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
        ValElement valElement;

        for(int i = 0, end = funcInfo.ArgInitLexems.Count; i < end; ++i)
        {
            if(funcInfo.ArgInitLexems[i].Count != 0)
            {
                ParseExpression(funcInfo.ArgInitLexems[i], 0, -1, ExpressionFlags.AllowDefaultValue, out valElement);
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

        StatementListElement statements = new StatementListElement();
        TreeElement element;

        m_currentBlock.Push(statements);

        if(funcInfo.FuncLexems.Count > 0)
        {
            for(int i = 0, end = funcInfo.FuncLexems.Count;;)
            {
                i = ParseStatement(funcInfo.FuncLexems, i, out element);
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
    /// <param name="lexems">List of lexems</param>
    /// <param name="pos">Position of current parsed lexem</param>
    /// <param name="elem">Statement builded on lexems. null if it's a block</param>
    /// <returns>Position of a next lexem</returns>
    private int ParseStatement(Lexems lexems, int pos, out TreeElement elem)
    {
        if(m_symbols.IsTypeExist(lexems[pos].source))
        {
            return ParseVarDeclaration(lexems, pos, out elem);
        }
        else if(lexems[pos].source == "{")
        {
            return ParseBlock(lexems, pos, out elem);
        }
        else if(lexems[pos].source == "}")
        {
            Compilation.Assert(m_currentBlock.Count > 0, "Unexpected '}'. Did you forget '{' ?", pos);

            m_currentBlock.Pop();
            elem = null;
            return ++pos;
        }
        else if(lexems[pos].source == "print")//TODO: remove it
        {
            ++pos;

            PrintCurrentValElement printElem = new PrintCurrentValElement();
            string varName = lexems[pos].source;
            printElem.VarGet = new VarGetSetValElement{ VarName = varName };

            ++pos;
            
            elem = printElem;

            return pos;
        }
        else
        {
            ValElement expr;  
            pos = ParseExpression(lexems, pos, -1, ExpressionFlags.OperationRequired | ExpressionFlags.AllowDefaultValue,
                                  out expr);
            elem = expr;

            ++pos;//skip ';'

            return pos;
        }
    }

    /// <summary>
    /// Add new block
    /// </summary>
    /// <param name="lexems">List of lexems</param>
    /// <param name="pos">Position of current parsed lexem</param>
    /// <param name="elem">Statement builded on lexems. Always null</param>
    /// <returns>Position of a next lexem</returns>
    private int ParseBlock(Lexems lexems, int pos, out TreeElement elem)
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
    /// <param name="lexems">List of lexems</param>
    /// <param name="pos">Position of current parsed lexem</param>
    /// <param name="elem">Element builded on lexems</param>
    /// <returns>Position of a next lexem</returns>
    private int ParseVarDeclaration(Lexems lexems, int pos, out TreeElement elem)
    {
        MultipleVarDeclarationElement multipleDecl = new MultipleVarDeclarationElement();

        VarDeclarationElement varDecl = new VarDeclarationElement();

        bool expectVar = true;
        bool hasInitialization = false;
        bool breakCycle = false;

        for(;;)
        {
            switch(lexems[pos].source)
            {
                case ",":
                {
                    Compilation.Assert(!expectVar, "Expected variable declaration, not comma", lexems[pos].line);

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

            varDecl.VarType = lexems[pos].source;
            Compilation.Assert(m_symbols.IsTypeExist(varDecl.VarType), "Unknown type '" + varDecl.VarType + "'", lexems[pos].line);
            
            ++pos;
            varDecl.VarName = lexems[pos].source;
            Compilation.Assert(lexems[pos].codeType == Lexem.CodeType.Name,
                               "Invalid variable name '" + varDecl.VarName + "'", lexems[pos].line);

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
            ValElement initElements;
            pos = ParseExpression(lexems, pos, -1, ExpressionFlags.AllowDefaultValue | ExpressionFlags.AllowAutoDefault,
                                  out initElements); 
            //TODO: auto default

            Compilation.Assert(multipleDecl.GetVars().Count == initElements.ValCount, 
                               "Each variable associated with only one expression (variables: " 
                               + multipleDecl.GetVars().Count +
                               ", initalization expressions: " + initElements.ChildrenCount(), lexems[pos-1].line);

            for(int i = 0, end = multipleDecl.GetVars().Count; i < end; ++i)
            {
                multipleDecl.GetVar(i).InitVal = (ValElement)initElements.Child(i);
            }

            Compilation.Assert(lexems[pos].source == ";", "Did you forget ';' ?", lexems[pos].line);
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
    /// <param name="lexems">List of lexems</param>
    /// <param name="pos">Position of current parsed lexem</param>
    /// <param name="endPos">Position at which function breaks. Pass -1 to parse until the end</param>
    /// <param name="flags">Control flags. ExpressionFlags.None to ignore all flags</param>
    /// <param name="elem"></param>
    /// <returns>Position of a next lexem</returns>
    private int ParseExpression(Lexems lexems, int pos, int endPos,
                                ExpressionFlags flags, 
                                out ValElement elem)
    {    
        Lexem currentLexem;

        OperationElement lastOperation = null;
        OperationElement rootOperation = null;
        ValElement lastValElement = null;

        bool breakCycle = false;

        if(endPos == -1)
        {
            endPos = lexems.Count;
        }

        for(int end = endPos; !breakCycle && pos < end;)
        {
            currentLexem = lexems[pos];

            switch(currentLexem.source)
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
                                            lexems[pos].line);
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
                            if(lexems[pos].source == "-")
                            {
                                rootOperation = lastOperation = new UnaryMinusElement();
                                ++pos;
                                continue;
                            }
                            else
                            {
                                Compilation.WriteError("Expected value or expression before '" 
                                                       + lexems[pos].source + "'", lexems[pos].line);
                            }
                        }
                        
                        OperationElement operation = null;
                        switch(lexems[pos].source)
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
                    pos = ParseExpressionValue(lexems, pos, flags | ExpressionFlags.AllowDefaultValue 
                                                                  & (~ExpressionFlags.OperationRequired),
                                               out lastValElement);
                    if(lastValElement == null)
                    {
                        Compilation.WriteError("Unknown symbol: '" + currentLexem.source + "'.", currentLexem.line);
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
            if(flags.HasFlag(ExpressionFlags.OperationRequired))
            {
                Compilation.WriteError("Expression must contain at least one operator", lexems[pos].line);
            }
            if(lastValElement == null)
            {
                Compilation.WriteError("No operation or value in expression", lexems[pos].line); 
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
    /// <param name="lexems">List of lexems</param>
    /// <param name="pos">Position of current parsed lexem</param>
    /// <param name="flags">Control flags. ExpressionFlags.None to ignore all flags</param>
    /// <param name="elem">Builded value element. null if it's not a value</param>
    /// <returns>Position of a next lexem</returns>
    private int ParseExpressionValue(Lexems lexems, int pos, ExpressionFlags flags, out ValElement elem) 
    {
        MultipleValElement multipleVals = new MultipleValElement();
        
        if(lexems[pos].source == "(")
        {
            ++pos;
            List<ValElement> values;

            pos = ExtractSeparatedExpressions(lexems, pos, flags, out values);

            if(pos >= (lexems.Count-1))
            {
                Compilation.WriteError("Unexpected end of file. Did you forget ')' and ';' ?", -1);
            }

            foreach(var i in values)
            {
                multipleVals.AddValue(i);
            }
        }//"("
        else if(lexems[pos].codeType == Lexem.CodeType.Number)
        {
            var val = new ConstValElement{ Type = m_symbols.GetTypeOfConstVal(lexems[pos].source),
                                            Value = lexems[pos].source };
            ++pos;
            elem = val;
            return pos;
        }
        else if(m_currentFunction.LocalVars.Any(local => local.VarName == lexems[pos].source))
        {
            var val = new VarGetSetValElement{ VarName = lexems[pos].source };

            ++pos;
            elem = val;
            return pos;
        }
        else if(m_foundedFunctions.Any(funcInfo => funcInfo.Info.Name == lexems[pos].source))
        {
            FunctionCallElement callElement = new FunctionCallElement();
            
            ++pos;
            List<ValElement> values;

            pos = ExtractSeparatedExpressions(lexems, pos, 
                                              flags | ExpressionFlags.AllowAutoDefault,
                                              out values);

            for(int i = 0, end = values.Count; i < end; ++i)
            {
                if(values[i] == null)
                {
                    //TODO: finish
                    //TODO: all value elements must contain ResultType
                }
            }
        }
        else if(lexems[pos].source == "default")
        {
            ++pos;

            if(lexems[pos].source == "(")
            {
                if(!flags.HasFlag(ExpressionFlags.AllowAutoDefault))
                {
                    Compilation.Assert(flags.HasFlag(ExpressionFlags.AllowDefaultValue), 
                                       "Default type value keyword is used but it isn't allowed here.", lexems[pos].line);
                }

                ++pos;//skip '('

                string typeName = lexems[pos].source;//TODO: namespaces

                ConstValElement constVal = m_symbols.GetDefaultVal(typeName);

                elem = new ConstValElement{ Type = constVal.Type, Value = constVal.Value };

                ++pos;
                ++pos;//skip ')'
            }
            else
            {
                Compilation.Assert(flags.HasFlag(ExpressionFlags.AllowAutoDefault), 
                                   "Auto default keyword is used but it isn't allowed here.", lexems[pos].line);
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
    private void InsertValInTree(OperationElement operation, ValElement value, bool right = true)
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
    /// <param name="lexems">List of lexems</param>
    /// <param name="pos">Position of current parsed lexem</param>
    /// <param name="flags">Control flags. ExpressionFlags.None to ignore all flags</param>
    /// <param name="elements">Builded expressions</param>
    /// <returns>Position of a next lexem</returns>
    private int ExtractSeparatedExpressions(Lexems lexems, int pos, ExpressionFlags flags,
                                            out List<ValElement> elements)
    {
        elements = new List<ValElement>();
        ValElement val;
        int currentLevel = 1;

        for(int i = pos; currentLevel != 0 && i < lexems.Count;)
        {
            switch(lexems[i].source)
            {
                case ";":
                    Compilation.WriteError("Unexpected symbol ';'. Did you forget ')' ?", lexems[i].line);
                    break;
                case "(":
                    ++currentLevel;
                    ++i;
                    continue;
                case ")":
                    --currentLevel;
                    if(currentLevel == 0)
                    {
                        pos = i = ParseExpression(lexems, pos, i, flags, out val) + 1;
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
                        pos = i = ParseExpression(lexems, pos, i, flags, out val) + 1;
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