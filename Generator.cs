using System;
using System.Collections.Generic;

/// <summary>
/// Instruction codes
/// </summary>
public enum GenCodes
{
    NewLVar, SetLVarVal, GetLVarVal,
    Push, Pop,
    Add, Subtract, Multiply, Divide, Exponent, Negate,
    CallFunc,
    Meta, Print/*temporary*/
}

/// <summary>
/// Instruction
/// </summary>
public sealed class GenOp
{
    public GenCodes Code;
    public int ArgCount;
    public IList<byte> Bytes;
}

/// <summary>
/// It's passed from generator to interpreter
/// </summary>
public sealed class GeneratorOutput
{
    public List<GenOp> Operations;
}

/// <summary>
/// Converts program tree to list of instructions
/// </summary>
public sealed class Generator
{
    /// <summary>
    /// Generate instructions using program tree
    /// </summary>
    /// <param name="parserOutput"></param>
    public void Generate(ParserOutput parserOutput)
    {
        foreach(var func in parserOutput.Functions)
        {
            m_funcList.Add(func.Info.BuildName);
        }
        foreach(var func in parserOutput.Functions)
        {
            func.PrepareTree();
        }

        
        foreach(var func in parserOutput.Functions)
        {
            m_currentOperations = new List<GenOp>();
            m_funcOffsets.Add(m_currentOpOffset);

            func.GenLowLevel(this);
            m_currentOpOffset += m_currentOperations.Count;
            m_operations.Add(m_currentOperations);
            
            m_currentOperations = null;
        }

        m_output.Operations = BuildOperations();
    }

    /// <summary>
    /// Returns generator output
    /// </summary>
    /// <returns></returns>
    public GeneratorOutput GetOutput()
    {
        return m_output;
    } 

    public int GetFunctionIndex(string name)
    {
        int index = m_funcList.IndexOf(name);

        if(index == -1)
        {
            Compilation.WriteCritical("Function isn't registered in generator. It's a bug");
        }

        return index;
    }

    /// <summary>
    /// Returns unique number of variable
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int GetLocalVarIndex(string name)//TODO: add unique variable id (maybe line?)
    {
        int index = m_localVars.IndexOf(name);

        if(index == -1)
        {
            m_localVars.Add(name);
            index = m_localVars.Count - 1;
        }

        return index;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public void RemoveLocalVariable(string name)//TODO: remove?
    {
        m_localVars.Remove(name);
    }

    /// <summary>
    /// Reset variables' id
    /// </summary>
    public void ResetLocalVarCounter()
    {
        m_localVars.Clear();
    }

    /// <summary>
    /// Adds new instruction
    /// </summary>
    /// <param name="op"></param>
    public void AddOp(GenOp op)
    {
        m_currentOperations.Add(op);
    }

    /// <summary>
    /// Adds new instruction
    /// </summary>
    /// <param name="code"></param>
    /// <param name="argCount"></param>
    /// <param name="bytes"></param>
    public void AddOp(GenCodes code, int argCount, IList<byte> bytes)
    {
        m_currentOperations.Add(new GenOp{ Code = code, ArgCount = argCount, Bytes = bytes });
    }

    private List<GenOp> BuildOperations()
    {
        List<GenOp> operations = new List<GenOp>();

        foreach(var i in m_operations)
        {
            operations.AddRange(i);
        }

        int funcIndex;
        foreach(var op in operations)
        {
            if(op.Code == GenCodes.CallFunc)
            {
                funcIndex = ByteConverter.New(op.Bytes).GetInt32();
                op.Bytes = ByteConverter.New().CastInt32(m_funcOffsets[funcIndex]).Bytes;
            }
        }

        return operations;
    }

    private List<List<GenOp>> m_operations = new List<List<GenOp>>();
    private List<GenOp> m_currentOperations = new List<GenOp>();
    private List<string> m_localVars = new List<string>();
    private List<string> m_funcList = new List<string>();
    private List<int> m_funcOffsets = new List<int>();

    private int m_currentOpOffset = 0;

    private GeneratorOutput m_output = new GeneratorOutput();
}