using System.Collections.Generic;
using System.Linq;

namespace ALang
{
    public class FunctionElement : TreeElement
    {
        public LanguageFunction Info;

        public override int RequiredSpaceInLocals => Child(0).RequiredSpaceInLocals;

        public override void PrepareBeforeGenerate()
        {
        }

        public override void GenerateInstructions(Generator generator)
        {
            generator.ResetLocalVarCounter();

            generator.AddOp(GenCodes.UpdateFunc, 0, null);

            foreach (var child in m_children)
            {
                child.GenerateInstructions(generator);
            }

            generator.AddOp(GenCodes.FuncEnd, 0, null);
        }

        public int GetFunctionRequiredSpaceForVariables()
        {
            return m_localVars.Select(val => LanguageSymbols.Instance.GetTypeSize(val.VarType))
                       .Aggregate(0, (val1, val2) => val1 + val2, result => result)
                   + m_argumentsVars.Select(val => LanguageSymbols.Instance.GetTypeSize(val.VarType))
                       .Aggregate(0, (val1, val2) => val1 + val2, result => result);
        }

        public void AddLocalVariable(VarDeclarationElement elem)
        {
            m_localVars.Add(elem);

            m_currentNeededLocalVarsSpace += LanguageSymbols.Instance.GetTypeSize(elem.VarType);

            if (m_currentNeededLocalVarsSpace > m_maxNeededLocalVarsSpace)
            {
                m_maxNeededLocalVarsSpace = m_currentNeededLocalVarsSpace;
            }
        }

        public VarDeclarationElement GetLocalVariable(string type, string name)
        {
            var elem = m_localVars.Find(v => v.VarType == type && v.VarName == name);

            if (elem == null)
            {
                Compilation.WriteError(string.Format("Variable '{0}' wasn't found in function '{1}'",
                        name, Info.BuildName),
                    -1);
            }

            return elem;
        }

        public void PopLocalVarStackSpace(string type, string name)
        {
            var elem = m_localVars.Find(v => v.VarType == type && v.VarName == name);

            if (elem == null)
            {
                Compilation.WriteError(string.Format("Variable '{0}' wasn't found in function '{1}'",
                        name, Info.BuildName),
                    -1);
            }

            m_currentNeededLocalVarsSpace -= LanguageSymbols.Instance.GetTypeSize(type);
        }

        public void AddArgumentVariable(VarDeclarationElement elem)
        {
            m_argumentsVars.Add(elem);
        }

        public VarDeclarationElement GetArgumentVariable(string type, string name)
        {
            var elem = m_argumentsVars.Find(v => v.VarType == type && v.VarName == name);

            if (elem == null)
            {
                Compilation.WriteError(string.Format("Variable '{0}' wasn't found in function '{1}'",
                        name, Info.BuildName),
                    -1);
            }

            return elem;
        }

        public bool HasVariable(string name)
        {
            return m_argumentsVars.Any(variable => variable.VarName == name) ||
                   m_localVars.Any(variable => variable.VarName == name);
        }

        public VarDeclarationElement GetVariable(string name)
        {
            VarDeclarationElement elem;
            elem = m_argumentsVars.Find(variable => variable.VarName == name);

            if (elem != null)
                return elem;

            elem = m_localVars.Find(variable => variable.VarName == name);

            if (elem != null)
                return elem;

            Compilation.WriteError(string.Format("Variable '{0}' wasn't found in function '{1}'",
                    name, Info.BuildName),
                -1);
            return null;
        }

        private List<VarDeclarationElement> m_localVars = new List<VarDeclarationElement>();
        private List<VarDeclarationElement> m_argumentsVars = new List<VarDeclarationElement>();
        private int m_currentNeededLocalVarsSpace;
        private int m_maxNeededLocalVarsSpace;
    }
}