namespace ALang
{
    public sealed class SingleGenOpElement : TreeElement
    {
        public GenOp Operation { get; set; }

        public override void GenerateInstructions(Generator generator)
        {
            generator.AddOp(Operation);
        }
    }
}