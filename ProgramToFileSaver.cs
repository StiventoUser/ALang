using System;
using System.IO;
using System.Linq;

namespace ALang
{
    public sealed class ProgramToFileSaver
    {
        public GeneratorOutput Program { get; set; }

        public void Save(string path)
        {
            if (Program == null)
            {
                //TODO: error
                return;
            }

            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                writer.Write("ALang".Select(ch => (byte) ch).ToArray());

                writer.Write((Int32) 0); //header size

                writer.Write((Int32) Program.OperationsByteSize);
                foreach (var operation in Program.Operations)
                {
                    writer.Write((Int32) operation.Code);
                    if (operation.ArgCount > 0)
                        writer.Write(operation.Bytes.ToArray());
                }
            }
        }
    }
}