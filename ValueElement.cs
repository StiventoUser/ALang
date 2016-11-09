using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ElementResultType//TODO: Operation result
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
    public bool IsGet
    {
        get { return m_isGet; }
        set
        {
            m_isGet = value;
            foreach(var child in m_children)
            {
                (child as ValElement).IsGet = value;
            }
        }
    }
    public bool IsSet
    {
        get { return m_isSet; }
        set
        {
            m_isSet = value;
            foreach(var child in m_children)
            {
                (child as ValElement).IsSet = value;
            }
        }
    }
    public ElementResultType Result = new ElementResultType();
    public abstract void GenerateValue(Generator generator, int index);

    private bool m_isGet = false;
    private bool m_isSet = false; 
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

    public override void GenerateValue(Generator generator, int index)
    {
        generator.AddOp(GenCodes.Push, 2, 
            ByteConverter.New().Cast(1/*type*/).Cast(int.Parse(Value)).Bytes);
    }
    public override void GenLowLevel(Generator generator)
    { 
        GenerateValue(generator, 0);
    }
}
public class VarGetSetValElement : ValElement
{
    public override int ValCount{ get{ return 1; } }

    public string VarName{get;set;}

    public override void GenerateValue(Generator generator, int index)
    {
        if(IsSet)
        {
            generator.AddOp(GenCodes.SetLVarVal, 1,
                ByteConverter.New().Cast(generator.GetLocalVarIndex(VarName)).Bytes);
        }
        if(IsGet)
        {
            generator.AddOp(GenCodes.GetLVarVal, 1, 
                ByteConverter.New().Cast(generator.GetLocalVarIndex(VarName)).Bytes);
        }
    }
    public override void GenLowLevel(Generator generator)
    { 
        GenerateValue(generator, 0);
    }
}
public class FunctionCallElement : ValElement
{
    public override int ValCount{ get{ return CallArguments.Count; } }

    public LanguageFunction FunctionInfo{get;set;}
    public List<ValElement> CallArguments{get;set;}

    public override void GenerateValue(Generator generator, int index)
    {
        
    }
    public override void GenLowLevel(Generator generator)
    { 
        GenerateValue(generator, 0);
    }
}
public class MultipleValElement : ValElement//TODO first: generating values, second: execute operations
{
    public override int ValCount{ get{ return m_values.Count; } }
    public bool IsGeneratedReverse{get;set;} = true;

    public void AddValue(ValElement elem)
    {
        m_values.Add(elem);
        AddChild(elem);
    }
    public List<ValElement> GetValues()
    {
        return m_values;
    }
    private List<ValElement> m_values = new List<ValElement>();

    public override void GenerateValue(Generator generator, int index)
    {
        m_values[index].GenerateValue(generator, index);
    }
    public override void GenLowLevel(Generator generator)
    { 
        for(int i = 0, end = ValCount; i < end; ++i)
        {
            GenerateValue(generator, i);
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
        return Vars;//TODO: AsReadOnly
    } 
    private List<VarDeclarationElement> Vars = new List<VarDeclarationElement>();
}