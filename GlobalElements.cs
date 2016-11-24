using System;
using System.Collections.Generic;
using System.Linq;

public class FunctionElement : TreeElement
{
    public LanguageFunction Info;

    public override void PrepareBeforeGenerate()
    {
    }
    public override void GenLowLevel(Generator generator)
    {
        generator.ResetLocalVarCounter();

        generator.AddOp(GenCodes.Func, 1, 
                ByteConverter.New().CastInt32(
                   LocalVars.Select(val => LanguageSymbols.Instance.GetTypeSize(val.VarType))
                            .Aggregate((val1, val2) => val1 + val2))
                            .Bytes);

        foreach(var child in m_children)
        {
            child.GenLowLevel(generator);
        }

        generator.AddOp(GenCodes.FuncEnd, 0, null);
    }

    public List<VarDeclarationElement> LocalVars = new List<VarDeclarationElement>();
    public List<VarDeclarationElement> ArgumentsVars = new List<VarDeclarationElement>();
}
