using System;
using System.Collections.Generic;

public class FunctionElement : TreeElement
{
    public override void GenLowLevel(Generator generator)
    {
        generator.ResetLocalVarCounter();

        foreach(var child in m_children)
        {
            child.GenLowLevel(generator);
        }
    }

    public List<VarDeclarationElement> LocalVars = new List<VarDeclarationElement>();
}
