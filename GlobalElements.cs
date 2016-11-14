using System;
using System.Collections.Generic;

public class FunctionElement : TreeElement
{
    public LanguageFunction Info;

    public override void PrepareBeforeGenerate()
    {
        VarDeclarationElement declElem;

        foreach(var arg in Info.Arguments)
        {
            declElem = new VarDeclarationElement{ VarType = arg.TypeInfo.Name, 
                                                  VarName = arg.ArgName,
                                                  IsInitGeneratedBefore = true };
            LocalVars.Add(declElem);
            m_argumentsVars.Add(declElem);
        }
    }
    public override void GenLowLevel(Generator generator)
    {
        generator.ResetLocalVarCounter();

        foreach(var arg in m_argumentsVars)
        {
            arg.GenLowLevel(generator);
        }

        foreach(var child in m_children)
        {
            child.GenLowLevel(generator);
        }
    }

    public List<VarDeclarationElement> LocalVars = new List<VarDeclarationElement>();
    private List<VarDeclarationElement> m_argumentsVars = new List<VarDeclarationElement>();
}
