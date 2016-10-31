using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ElementResultType
{
    public bool IsFunction;

    //Not Function
    public string ResultType;

    //Function
    public LanguageFunction FunctionType;
    //public List<ElementResultTypePart> ResultTypes = new List<ElementResultTypePart>();
}

public abstract class ValElement : TreeElement
{
    public abstract int ValCount{ get; }
    public bool IsGet{get;set;} = false;
    public bool IsSet{get;set;} = false;
    public ElementResultType Result = new ElementResultType();
}
public class ConstValElement : ValElement
{
    public override int ValCount{ get{ return 1; } }
    public override bool IsCompileTime
    {
        get{ return true; }
    }
    public string Type;
    public string Value;

    public ConstValElement()
    {
        Result = new ElementResultType{ IsFunction = false, ResultType = Type };
    }

    public override void GenLowLevel(Generator generator)
    {
        generator.AddOp(GenCodes.Push, 2, ByteConverter.New().Cast(1/*type*/).Cast(int.Parse(Value)).Bytes);
    }

}
public class VarGetSetValElement : ValElement
{
    public override int ValCount{ get{ return 1; } }

    public string VarName;

    public override void GenLowLevel(Generator generator)
    {
        if(IsSet)
        {
            generator.AddOp(GenCodes.SetLVarVal, 1, ByteConverter.New().Cast(generator.GetLocalVarIndex(VarName)).Bytes);
        }
        if(IsGet)
        {
            generator.AddOp(GenCodes.GetLVarVal, 1, ByteConverter.New().Cast(generator.GetLocalVarIndex(VarName)).Bytes);
        }
    }
}
public class MultipleValElement : ValElement
{
    public override int ValCount{ get{ return m_values.Count; } }
    public bool IsGeneratedReverse{get;set;} = true;

    public void AddValueVoid(ValElement elem)
    {
        m_values.Add(elem);
        AddChild(elem);
    }
    public List<ValElement> GetValues()
    {
        return m_values;
    }
    private List<ValElement> m_values = new List<ValElement>();

    public override void GenLowLevel(Generator generator)
    {
        IEnumerable<ValElement> enumVars = m_values;
        if(IsGeneratedReverse)
        {
            enumVars = enumVars.Reverse();
        }
        
        foreach(var values in enumVars)
        {
            values.GenLowLevel(generator);
        }
    }
}

public class VarDeclarationElement : TreeElement
{
    public string VarType;
    public string VarName;
    public ValElement InitVal;

    public override void GenLowLevel(Generator generator)
    {
        InitVal.GenLowLevel(generator);

        generator.AddOp(GenCodes.NewLVar, 1, ByteConverter.New().Cast(generator.GetLocalVarIndex(VarName)).Bytes);
    }
    public void GenLowLevelDelete(Generator generator)
    {
        //generator.AddOp(GenCodes.DeleteLVar, 0, null);
        //generator.RemoveLocalVariable(VarName);
    }
}
public class MultipleVarDeclarationElement : TreeElement
{
    public override void GenLowLevel(Generator generator)
    { 
        foreach(var i in Vars)
        {
            i.GenLowLevel(generator);
        }
    }
    public void AddVar(VarDeclarationElement elem)
    {
        Vars.Add(elem);
        AddChild(elem);
    }
    public VarDeclarationElement GetVar(int i)
    {
        return Vars[i];
    }
    public List<VarDeclarationElement> GetVars()
    {
        return Vars;//TODO AsReadOnly
    } 
    private List<VarDeclarationElement> Vars = new List<VarDeclarationElement>();
}