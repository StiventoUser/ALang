using System.Collections.Generic;

public class StatementListElement : TreeElement
{
    public override void GenLowLevel(Generator generator)
    {
        foreach(var child in m_children)
        {
            child.GenLowLevel(generator);
        }

        foreach(var variable in LocalVariables)
        {
            variable.GenLowLevelDelete(generator);
        }
    }

    public List<VarDeclarationElement> LocalVariables = new List<VarDeclarationElement>();
}