using System;
using System.Collections.Generic;
using System.Linq;

//TODO: check int8 is signed or unsigned Alang type in C# and C++

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
    Meta, Print/*temporary*/,
    Exit, Abort
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
    public Int32 OperationsByteSize;
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
        Func<List<GenOp>, int> findByteOffset = 
            ops =>
            {
                return ops.Select(op => sizeof(Int32) + op.Bytes.Count())
                          .Aggregate((val1, val2) => val1 + val2);
            };

        //Move !_Main() at beginning of a program
        {
            int i = parserOutput.Functions.FindIndex(f => f.Info.Name == "!_Main");

            var func = parserOutput.Functions[i];
            parserOutput.Functions.RemoveAt(i);
            parserOutput.Functions.Insert(0, func);
        }

        foreach(var func in parserOutput.Functions)
        {
            m_funcList.Add(func.Info.BuildName);
        }
        foreach(var func in parserOutput.Functions)
        {
            func.PrepareTree();
        }

        m_currentOpOffset += m_reservedInstructionsByteSize;
        
        foreach(var func in parserOutput.Functions)
        {
            m_currentOperations = new List<GenOp>();
            m_funcOffsets.Add(m_currentOpOffset);

            func.GenLowLevel(this);
            FixFunctionOperations();

            m_currentOpOffset += findByteOffset(m_currentOperations);
            m_operations.Add(m_currentOperations);
            
            m_currentOperations = null;
        }

        m_output.Operations = BuildProgramOperations();
        m_output.OperationsByteSize = m_currentOpOffset;
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
            int offset;
            if(m_localVars.Count == 0)
            {
                offset = 0;
            }
            else
            {
                offset = m_localVars.Last().Offset + LanguageSymbols.Instance.GetTypeSize(m_localVars.Last().Type);
            } 
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
        if(op.ArgCount == 0 || op.Bytes == null)
        {
            op.ArgCount = 0;
            op.Bytes = new List<byte>();
        }
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
        AddOp(new GenOp{ Code = code, ArgCount = argCount, Bytes = bytes });
    }

    private void FixFunctionOperations()
    {
        Int32 index, size;
        foreach(var op in m_currentOperations)
        {
            switch(op.Code)
            {
            case GenCodes.NewLVar:
            case GenCodes.GetLVarVal:
            case GenCodes.SetLVarVal:
            {   
                var converter = ByteConverter.New(op.Bytes.ToArray());
                index = converter.GetInt32();
                size = converter.GetInt32();

                op.Bytes = ByteConverter.New()
                                        .CastInt32(m_localVars[index].Offset)
                                        .CastInt32(size)
                                        .Bytes;
            }
                break;
            }
        }
    }
    private List<GenOp> BuildProgramOperations()
    {
        List<GenOp> operations = new List<GenOp>();

        foreach(var i in m_operations)
        {
            operations.AddRange(i);
        }

        Int32 index, size;
        foreach(var op in operations)
        {
            switch(op.Code)
            {
            case GenCodes.CallFunc:
            {
                var converter = ByteConverter.New(op.Bytes.ToArray());
                index = converter.GetInt32();
                size = converter.GetInt32();

                op.Bytes = ByteConverter.New().CastInt32(m_funcOffsets[index]).CastInt32(size).Bytes;
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

    private int m_reservedInstructionsByteSize = 0;//TODO: change to opList
}