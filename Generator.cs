using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Instruction codes
/// </summary>
public enum GenCodes : Int32
{
    NewLVar, SetLVarVal, GetLVarVal,
    Push, Pop,
    Add, Subtract, Multiply, Divide, Exponent, Negate,
    Func,
    CallFunc, FuncEnd, FuncReturn,
    Meta, Print/*temporary*/
};

/// <summary>
/// Instruction
/// </summary>
public sealed class GenOp
{
    public GenCodes Code;
    public int ArgCount;
    public IEnumerable<byte> Bytes;
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
    public int GetLocalVarIndex(string type, string name)
    {
        int index = m_localVars.FindIndex(variable => variable.Name == name);

        if(index == -1)
        {
            m_localVars.Add(new LocalVarInfo{ Type = "Int32", Name = "__stackState", Offset = 0 });//TODO: string type to enum

            int offset = m_localVars.Last().Offset + LanguageSymbols.Instance.GetTypeSize(m_localVars.Last().Type);
            m_localVars.Add(new LocalVarInfo{ Type = type, Name = name, Offset = offset });
            index = m_localVars.Count - 1;
        }

        return index;
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

        operations.Add(new GenOp{ Code = GenCodes.CallFunc, ArgCount = 1, 
                                  Bytes = ByteConverter.New().CastInt32(1).Bytes });//Function declared at next instruction

        foreach(var i in m_operations)
        {
            //operations.Add(new GenOp{ Code = GenCodes.NewLVar, ArgCount = 1, Bytes = ByteConverter.New().CastInt32(0).Bytes });
            
            operations.AddRange(i);

            //operations.Add(new GenOp{ Code = GenCodes.GetLVarVal, ArgCount = 1, Bytes = ByteConverter.New().CastInt32(0).Bytes });
            //operations.Add(new GenOp{ Code = GenCodes.MoveStack, ArgCount = 0 });
        }

        int index;
        foreach(var op in operations)
        {
            switch(op.Code)
            {
            case GenCodes.CallFunc:
            {
                index = BitConverter.ToInt32(op.Bytes.ToArray(), 0);
                op.Bytes = ByteConverter.New().CastInt32(m_funcOffsets[index]).Bytes;
            }
                break;
            case GenCodes.NewLVar:
            case GenCodes.GetLVarVal:
            {   
                index = BitConverter.ToInt32(op.Bytes.ToArray(), 0);
                op.Bytes = ByteConverter.New().CastInt32(m_localVars[index].Offset).Bytes;
            }
                break;
            }
        }

        return operations;
    }

    private class LocalVarInfo
    {
        public string Type;
        public string Name;
        public int Offset;
    }
    private List<List<GenOp>> m_operations = new List<List<GenOp>>();
    private List<GenOp> m_currentOperations = new List<GenOp>();
    private List<LocalVarInfo> m_localVars = new List<LocalVarInfo>();
    private List<string> m_funcList = new List<string>();
    private List<int> m_funcOffsets = new List<int>();

    private int m_currentOpOffset = 0;

    private GeneratorOutput m_output = new GeneratorOutput();
}