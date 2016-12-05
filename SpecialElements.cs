using System;

public sealed class SingleGenOpElement : TreeElement
{
    public GenOp Operation{get;set;}

    public override void GenLowLevel(Generator generator)
    {
        generator.AddOp(Operation);
    }
}