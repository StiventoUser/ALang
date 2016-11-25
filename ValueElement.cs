using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ElementResultTypePart
{
    public bool IsFunction;

    //Not Function
    public string ResultType;

    //TODO: Function result
    //Function
    //public LanguageFunction FunctionType;
    //public List<ElementResultTypePart> ResultTypes = new List<ElementResultTypePart>();

    public static bool operator==(ElementResultTypePart left, ElementResultTypePart right)
    {
        return left.ResultType == right.ResultType;
    }
    public static bool operator!=(ElementResultTypePart left, ElementResultTypePart right)
    {
        return left.ResultType != right.ResultType;
    }
    public override bool Equals(object o)
    {
        var other = o as ElementResultTypePart;
        if(o == null)
            return false;

        return this == other;
    }
    public override int GetHashCode()
    {
        int hash = 13;
        hash = (hash * 7) + IsFunction.GetHashCode();
        hash = (hash * 7) + ResultType.GetHashCode();

        return hash;
    }
}
public sealed class ElementResultType
{
    public List<ElementResultTypePart> ResultTypes;

    public static ElementResultType Create(params ElementResultTypePart[] elements)
    {
        return new ElementResultType{ ResultTypes = elements.ToList() };
    }
    public static ElementResultType Create(IEnumerable<ElementResultTypePart> elements)
    {
        return new ElementResultType{ ResultTypes = elements.ToList() };
    }
}

public abstract class ValueElement : TreeElement
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
                (child as ValueElement).IsGet = value;
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
                (child as ValueElement).IsSet = value;
            }
        }
    }
    public ElementResultType Result
    {
        get
        {
            if(m_result == null)
            {
                UpdateResultValue();
            }

            return m_result;
        }
        protected set
        {
            m_result = value;
        }
    }
    public abstract void GenerateValue(Generator generator, int index);

    protected abstract void UpdateResultValue();

    private ElementResultType m_result = null;

    private bool m_isGet = false;
    private bool m_isSet = false; 
}
public class ConstValElement : ValueElement 
{
    public override int ValCount{ get{ return 1; } }
    public override bool IsCompileTime
    {
        get{ return true; }
    }
    public string Type{get;set;}
    public string Value
    {
        get{ return m_value; }
        set
        {
            m_value = value;

            if(LanguageSymbols.Instance != null)
            {
                Type = LanguageSymbols.Instance.GetTypeOfConstVal(m_value);

                UpdateResultValue();
            }
        }
    }
 

    public override void GenerateValue(Generator generator, int index)
    {
        generator.AddOp(GenCodes.Push, 2, 
            ByteConverter.New().CastInt32(LanguageSymbols.Instance.GetTypeSize(Type))
                               .CastInt32(int.Parse(Value)).Bytes);
    }
    public override void GenLowLevel(Generator generator)
    { 
        GenerateValue(generator, 0);
    }

    public override void PrepareBeforeGenerate()
    {
        UpdateResultValue();
    }
    protected override void UpdateResultValue()
    {
        Result = ElementResultType.Create(new ElementResultTypePart{ ResultType = Type });
    }

    private string m_value;
}
public class VarGetSetValElement : ValueElement
{
    public override int ValCount{ get{ return 1; } }

    public string VarType
    {
        get{ return m_varType; }
        set
        {
            m_varType = value;
            
            UpdateResultValue();
        }
    }
    public string VarName{get;set;}

    public override void GenerateValue(Generator generator, int index)
    {
        if(IsSet)
        {
            generator.AddOp(GenCodes.SetLVarVal, 2,
                ByteConverter.New().CastInt32(generator.GetLocalVarIndex(VarType, VarName))
                                   .CastInt32(LanguageSymbols.Instance.GetTypeSize(VarType))
                                   .Bytes);
        }
        if(IsGet)
        {
            generator.AddOp(GenCodes.GetLVarVal, 2, 
                ByteConverter.New().CastInt32(generator.GetLocalVarIndex(VarType, VarName))
                                   .CastInt32(LanguageSymbols.Instance.GetTypeSize(VarType))
                                   .Bytes);
        }
    }

    public override void PrepareBeforeGenerate()
    {
        var funcElem = RootParent<FunctionElement>();
        var variable = funcElem.LocalVars.Find(elem => elem.VarName == VarName);

        Compilation.Assert(variable != null, "Variable '" + VarName + "' isn't exist", -1);

        VarType = variable.VarType;

        UpdateResultValue();
    }
    public override void GenLowLevel(Generator generator)
    { 
        GenerateValue(generator, 0);
    }

    protected override void UpdateResultValue()
    {
        Result = ElementResultType.Create(new ElementResultTypePart{ ResultType = VarType });
    }

    private string m_varType;
}
public class FunctionCallElement : ValueElement
{
    public override int ValCount{ get{ return CallArguments.Count; } }

    public LanguageFunction FunctionInfo
    {
        get{ return m_langFunction; }
        set
        {
            m_langFunction = value;
            UpdateResultValue();
        }
    }
    public List<ValueElement> CallArguments{get;set;}

    public override void GenerateValue(Generator generator, int index)
    {
        foreach(ValueElement i in Enumerable.Reverse(CallArguments))
        {
            i.GenLowLevel(generator);
        }

        generator.AddOp(GenCodes.CallFunc, 1, ByteConverter
                                                .New()
                                                .CastInt32(generator.GetFunctionIndex(FunctionInfo.BuildName))
                                                .Bytes);
    }

    public override void PrepareBeforeGenerate()
    {
        UpdateResultValue();
    }
    public override void GenLowLevel(Generator generator)
    { 
        GenerateValue(generator, 0);
    } 

    protected override void UpdateResultValue() 
    {
        Result = ElementResultType.Create(FunctionInfo.ReturnTypes.Select(
                                                info => new ElementResultTypePart{ ResultType = info.Name }
                                            ).ToList());
    }

    private LanguageFunction m_langFunction;  
}
public class MultipleValElement : ValueElement//TODO first: generating values, second: execute operations
{
    public override int ValCount{ get{ return m_values.Count; } }
    public bool IsGeneratedReverse{get;set;} = true;

    public void AddValue(ValueElement elem)
    {
        m_values.Add(elem);
        AddChild(elem);
    }
    public List<ValueElement> GetValues()
    {
        return m_values;
    }

    public override void PrepareBeforeGenerate()
    {
        UpdateResultValue();
    }
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

    protected override void UpdateResultValue()
    {
        Result = ElementResultType.Create(m_values.Select(val => val.Result.ResultTypes[0]));
    }

    private List<ValueElement> m_values = new List<ValueElement>();
}

public class VarDeclarationElement : TreeElement 
{
    public string VarType;
    public string VarName;

    public bool IsInitGeneratedBefore;
    public ValueElement InitVal;

    public override void GenLowLevel(Generator generator)
    {
        if(!IsInitGeneratedBefore)
        {
            InitVal.GenLowLevel(generator);
        }

        generator.AddOp(GenCodes.NewLVar, 2, 
            ByteConverter.New().CastInt32(generator.GetLocalVarIndex(VarType, VarName))
                               .CastInt32(LanguageSymbols.Instance.GetTypeSize(VarType))
                               .Bytes);
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