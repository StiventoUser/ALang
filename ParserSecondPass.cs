using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Lexems = System.Collections.Generic.List<Lexem>;

sealed public class ParserOutput
{
    public List<FunctionElement> Functions = new List<FunctionElement>();
}

public sealed partial class Parser
{
    public ParserOutput GetParserOutput()
    {
        return m_parserOutput;
    }

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

    private void ParseFunctionDefaultVals(Parser1To2Pass.FunctionInfo funcInfo)
    {
        ValElement valElement;

        for(int i = 0, end = funcInfo.ArgInitLexems.Count; i < end; ++i)
        {
            if(funcInfo.ArgInitLexems[i].Count != 0)
            {
                ParseExpression(funcInfo.ArgInitLexems[i], 0, out valElement, -1, false);
            }
            else
            {
                valElement = null;
            }

            funcInfo.Info.Arguments[i].DefaultVal = valElement;
        }
        m_symbols.UpdateFunction(funcInfo.Info);
    }
    private void ParseFunction(Parser1To2Pass.FunctionInfo funcInfo)
    {
        FunctionElement funcElem = new FunctionElement();
        m_currentFunction = funcElem;

        StatementListElement statements = new StatementListElement();
        TreeElement element;

        m_currentStatement.Push(statements);

        if(funcInfo.FuncLexems.Count > 0)
        {
            for(int i = 0, end = funcInfo.FuncLexems.Count;;)
            {
                i = ParseStatement(funcInfo.FuncLexems, i, out element);
                if(element != null)//if null => ignore (block)
                {
                    m_currentStatement.Peek().AddChild(element);
                }

                if(i >= end)
                    break;
            }   
        }

        funcElem.AddChild(statements);

        m_parserOutput.Functions.Add(funcElem);

        m_currentStatement.Pop();
    }

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
            m_currentStatement.Pop();
            elem = null;
            return ++pos;
        }
        else if(lexems[pos].source == "print")//TODO remove it
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
            pos = ParseExpression(lexems, pos, out expr);
            elem = expr;

            ++pos;//skip ';'

            return pos;
        }
    }
    private int ParseBlock(Lexems lexems, int pos, out TreeElement elem)
    {
        StatementListElement block = new StatementListElement();

        m_currentStatement.Peek().AddChild(block);
        m_currentStatement.Push(block);

        ++pos;//skip '{'

        elem = null;

        return pos;
    }
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
            pos = ParseExpression(lexems, pos, out initElements, -1, false); 

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
        m_currentStatement.Peek().LocalVariables.AddRange(multipleDecl.GetVars());

        return pos;
    }

    private int ParseExpression(Lexems lexems, int pos, out ValElement elem, int endPos = -1, bool requireOperation = true)
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
                    pos = ParseExpressionValue(lexems, pos, out lastValElement);
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
            if(requireOperation)
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
    private int ParseExpressionValue(Lexems lexems, int pos, out ValElement elem) 
    {
        MultipleValElement multipleVals = new MultipleValElement();
        
        if(lexems[pos].source == "(")
        {
            ValElement val;
            int currentLevel = 1;
            ++pos;
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
                            pos = i = ParseExpression(lexems, pos, out val, i, false) + 1;
                            multipleVals.AddValue(val);
                        }
                        else
                        {
                            ++i;
                        }
                        continue;
                    case ",":
                        if(currentLevel == 1)
                        {
                            pos = i = ParseExpression(lexems, pos, out val, i, false) + 1;
                            multipleVals.AddValue(val);
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
            if(pos >= (lexems.Count-1))
            {
                Compilation.WriteError("Unexpected end of file. Did you forget ')' and ';' ?", -1);
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
        //TODO function call
        else//Not a value
        {
            elem = null;
            return pos; 
        }

        elem = multipleVals;
        return pos;
    }

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

    private FunctionElement m_currentFunction;
    private Stack<StatementListElement> m_currentStatement = new Stack<StatementListElement>();
    private List<Parser1To2Pass.FunctionInfo> m_foundedFunctions = new List<Parser1To2Pass.FunctionInfo>();
    private ParserOutput m_parserOutput = new ParserOutput();
}