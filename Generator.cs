using System;
using System.Collections.Generic;

public enum GenCodes
{
    NewLVar, SetLVarVal, GetLVarVal,
    Push, Pop,
    Add, Subtract, Multiply, Divide, Exponent, Negate,
    Meta, Print/*temporary*/
}

public sealed class GenOp
{
    public GenCodes Code;
    public int ArgCount;
    public IList<byte> Bytes;
}

public sealed class GeneratorOutput
{
    public List<GenOp> Operations;
}

public sealed class Generator
{
    public void Generate(ParserOutput parserOutput)
    {
        foreach(var func in parserOutput.Functions)
        {
            //func.GenLowLevel(this);
        }
        parserOutput.Functions[0].GenLowLevel(this);//TODO normal code generation

        m_output.Operations = m_operations;
    }
    public GeneratorOutput GetOutput()
    {
        return m_output;
    }

    public int GetLocalVarIndex(string name)//TODO add unique variable id (maybe line?)
    {
        int index = m_localVars.IndexOf(name);

        if(index == -1)
        {
            m_localVars.Add(name);
            index = m_localVars.Count - 1;
        }

        return index;
    }
    public void RemoveLocalVariable(string name)//TODO remove?
    {
        m_localVars.Remove(name);
    }
    public void ResetLocalVarCounter()
    {
        m_localVars.Clear();
    }

    public void AddOp(GenOp op)
    {
        m_operations.Add(op);
    }
    public void AddOp(GenCodes code, int argCount, IList<byte> bytes)
    {
        m_operations.Add(new GenOp{ Code = code, ArgCount = argCount, Bytes = bytes });
    }

    private List<GenOp> m_operations = new List<GenOp>();
    private List<string> m_localVars = new List<string>();

    private GeneratorOutput m_output = new GeneratorOutput();
}