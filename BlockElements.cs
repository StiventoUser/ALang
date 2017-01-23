using System;
using System.Collections.Generic;
using System.Linq;

namespace ALang
{
    public class StatementListElement : TreeElement
    {
        public override int RequiredSpaceInLocals => Children().Max(child => child.RequiredSpaceInLocals);

        public override void GenerateInstructions(Generator generator)
        {
            FunctionElement func = RootParent<FunctionElement>();

            foreach (var child in m_children)
            {
                child.GenerateInstructions(generator);
            }

            if (m_localVariables.Count > 0)
            {
                foreach (var variable in m_localVariables)
                {
                    variable.GenLowLevelDelete(generator);
                }

                foreach (var variable in m_localVariables)
                {
                    func.PopLocalVarStackSpace(variable.VarType, variable.VarName);
                }

                generator.RemoveLastNLocalVars(m_localVariables.Count);
            }
        }

        public void AddLocalVariable(VarDeclarationElement elem)
        {
            m_localVariables.Add(elem);
        }

        private List<VarDeclarationElement> m_localVariables = new List<VarDeclarationElement>();
    }

    public class StatementElement : TreeElement
    {
        public override int RequiredSpaceInLocals => Child(0).RequiredSpaceInLocals;

        public override void GenerateInstructions(Generator generator)
        {
            Compilation.Assert(ChildrenCount() == 1,
                string.Format("BUG: Statement contains {0} elements (must 1)", ChildrenCount()), -1);

            //PrepareStatementRecursive(generator);

            Child(0).GenerateInstructions(generator);

            //CleanUpStatementRecursive(generator);
        }
    }
}