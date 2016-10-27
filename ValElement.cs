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
    public bool IsGet = false;
    public bool IsSet = false;

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
    public override int ValCount{ get{ return Values.Count; } }

    public void AddValueVoid(ValElement elem)
    {
        Values.Add(elem);
        AddChild(elem);
    }
    private List<ValElement> Values = new List<ValElement>();

    public override void GenLowLevel(Generator generator)
    {
        IEnumerable<ValElement> enumVars = Values;

        foreach(var values in enumVars.Reverse())
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