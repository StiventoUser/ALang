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
            //func.GenLowLevel(this);
        }
        parserOutput.Functions[0].GenLowLevel(this);//TODO: normal code generation

        m_output.Operations = m_operations;
    }

    /// <summary>
    /// Returns generator output
    /// </summary>
    /// <returns></returns>
    public GeneratorOutput GetOutput()
    {
        return m_output;
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
        m_operations.Add(op);
    }

    /// <summary>
    /// Adds new instruction
    /// </summary>
    /// <param name="code"></param>
    /// <param name="argCount"></param>
    /// <param name="bytes"></param>
    public void AddOp(GenCodes code, int argCount, IList<byte> bytes)
    {
        m_operations.Add(new GenOp{ Code = code, ArgCount = argCount, Bytes = bytes });
    }

    private List<GenOp> m_operations = new List<GenOp>();
    private List<string> m_localVars = new List<string>();

    private GeneratorOutput m_output = new GeneratorOutput();
}