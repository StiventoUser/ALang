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
                ByteConverter
                .New()
                .CastInt32(
                    LocalVars.Select(val => LanguageSymbols.Instance.GetTypeSize(val.VarType))
                            .Aggregate(0, (val1, val2) => val1 + val2, result => result)
                  + ArgumentsVars.Select(val => LanguageSymbols.Instance.GetTypeSize(val.VarType))
                            .Aggregate(0, (val1, val2) => val1 + val2, result => result))
                .Bytes);

        foreach(var child in m_children)
        {
            child.GenLowLevel(generator);
        }

        generator.AddOp(GenCodes.FuncEnd, 0, null);
    }

    public bool HasVariable(string name)
    {
        return ArgumentsVars.Any(variable => variable.VarName == name) || 
               LocalVars.Any(variable => variable.VarName == name);
    }
    public VarDeclarationElement GetVariable(string name)
    {
        VarDeclarationElement elem;
        elem = ArgumentsVars.Find(variable => variable.VarName == name);
        
        if(elem != null)
            return elem;

        elem = LocalVars.Find(variable => variable.VarName == name);
        
        if(elem != null)
            return elem;

        Compilation.WriteError(string.Format("Variable '{0}' wasn't found in function '{1}'",
                                             name, Info.BuildName),
                               -1);
        return null;
    }

    public List<VarDeclarationElement> LocalVars = new List<VarDeclarationElement>();
    public List<VarDeclarationElement> ArgumentsVars = new List<VarDeclarationElement>();
}
 