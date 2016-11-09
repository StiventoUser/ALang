using System;
using System.Collections.Generic;

public sealed class Interpreter
{
    public void Run(GeneratorOutput genOperations)
    {
        GenOp genOp;
        for(int i = 0, end = genOperations.Operations.Count; i < end; ++i)
        {
            genOp = genOperations.Operations[i]; 

            switch(genOp.Code)
            {
                case GenCodes.NewLVar:
                    m_localVars.Add(m_stack.Pop()); 
                    break; 
                case GenCodes.SetLVarVal:
                    m_localVars[ByteConverter.New(genOp.Bytes).GetInt32()] = m_stack.Pop();
                    break;
                case GenCodes.GetLVarVal:
                    m_stack.Push(m_localVars[ByteConverter.New(genOp.Bytes).GetInt32()]);
                    break;
                case GenCodes.Push:
                    m_stack.Push(ByteConverter.New(genOp.Bytes).SkipBytes(4).GetInt32());
                    break; 
                case GenCodes.Pop:
                    m_stack.Pop();
                    break;
                case GenCodes.Add:
                    {
                        m_stack.Push(m_stack.Pop() + m_stack.Pop());
                    }
                    break;
                case GenCodes.Subtract:
                    {
                        int b = m_stack.Pop(), a = m_stack.Pop();
                        m_stack.Push(a - b);
                    }
                    break;
                case GenCodes.Multiply:
                    {
                        m_stack.Push(m_stack.Pop() * m_stack.Pop());
                    }
                    break;
                case GenCodes.Divide:
                    {
                        int b = m_stack.Pop(), a = m_stack.Pop();
                        m_stack.Push(a / b);
                    }
                    break;
                case GenCodes.Negate:
                    m_stack.Push(-m_stack.Pop());
                    break;
                case GenCodes.Print://TODO: temporary
                    Console.WriteLine("Value: " + m_stack.Pop());
                    break;
            }
        }
        
    }
    Stack<int> m_stack = new Stack<int>();
    List<int> m_localVars = new List<int>();
}