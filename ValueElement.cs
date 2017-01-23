using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ALang
{
    public sealed class ElementResultType
    {
        public List<LanguageType> ResultTypes;

        public static ElementResultType Create(params LanguageType[] elements)
        {
            return new ElementResultType {ResultTypes = elements.ToList()};
        }

        public static ElementResultType Create(IEnumerable<LanguageType> elements)
        {
            return new ElementResultType {ResultTypes = elements.ToList()};
        }
    }

    public abstract class ValueElement : TreeElement
    {
        public abstract int NumberOfBranches { get; }

        public bool IsGet
        {
            get { return m_isGet; }
            set
            {
                m_isGet = value;
                foreach (var child in m_children)
                {
                    ((ValueElement) child).IsGet = value;
                }
            }
        }

        public bool IsSet
        {
            get { return m_isSet; }
            set
            {
                m_isSet = value;
                foreach (var child in m_children)
                {
                    ((ValueElement) child).IsSet = value;
                }
            }
        }

        public ElementResultType ResultType
        {
            get
            {
                if (m_result == null)
                {
                    UpdateResultType();
                }

                return m_result;
            }
            protected set { m_result = value; }
        }

        public virtual void GenerateForBranch(Generator generator, int branch)
        {
        }

        public void UpdateResultType()
        {
            foreach (ValueElement elem in Children<ValueElement>())
            {
                elem.UpdateResultType();
            }

            DoUpdateResultType();
        }

        protected abstract void DoUpdateResultType();

        private ElementResultType m_result = null;

        private bool m_isGet = false;
        private bool m_isSet = false;
    }

    public class ConstValElement : ValueElement
    {
        public override int NumberOfBranches
        {
            get { return 1; }
        }

        public override bool IsCompileTime
        {
            get { return true; }
        }

        public string Type { get; set; }

        public string Value
        {
            get { return m_value; }
            set
            {
                m_value = value;

                if(LanguageSymbols.Instance != null)
                {
                    UpdateResultType();
                }
            }
        }


        public override void GenerateForBranch(Generator generator, int branch)
        {
            var valContainer = new StringToValue {TypeName = Type, StringVal = Value};

            generator.AddOp(GenCodes.Push, 2,
                ByteConverter.New()
                    .CastInt32(LanguageSymbols.Instance.GetTypeSize(Type))
                    .CastValueContainer(valContainer)
                    .Bytes);
        }

        public override void GenerateInstructions(Generator generator)
        {
            GenerateForBranch(generator, -1);
        }

        public override void PrepareBeforeGenerate()
        {
            UpdateResultType();
        }

        protected override void DoUpdateResultType()
        {
            Type = LanguageSymbols.Instance.GetTypeOfConstVal(m_value);
            ResultType = ElementResultType.Create(LanguageSymbols.Instance.GetTypeByName(Type));
        }

        private string m_value;
    }

    public class GetVariableElement : ValueElement
    {
        public enum GenerateOptions
        {
            Address, Value
        };

        public override int NumberOfBranches
        {
            get { return 1; }
        }

        public string VarType
        {
            get { return m_varType; }
            set
            {
                m_varType = value;

                UpdateResultType();
            }
        }

        public string VarName { get; set; }

        public GenerateOptions ResultOfGeneration { get; set; }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            if (ResultOfGeneration == GenerateOptions.Value)
            {
                generator.AddOp(GenCodes.GetLVarVal, 2,
                    ByteConverter.New()
                        .CastInt32(generator.GetLocalVarAddress(VarType, VarName))
                        .CastInt32(LanguageSymbols.Instance.GetTypeSize(VarType))
                        .Bytes);
            }
            else
            {
                generator.AddOp(GenCodes.PushVarAddress, 1,
                    ByteConverter.New()
                        .CastInt64(generator.GetLocalVarAddress(VarType, VarName))
                        .Bytes);
            }
        }

        public override void GenerateInstructions(Generator generator)
        {
            GenerateForBranch(generator, -1);
        }

        public override void PrepareBeforeGenerate()
        {
            var funcElem = RootParent<FunctionElement>();
            var variable = funcElem.GetVariable(VarName);

            Compilation.Assert(variable != null, $"Variable '{VarName}' isn't exist", -1);

            if (variable != null)
            {
                VarType = variable.VarType;
            }
            else
            {
                Compilation.WriteCritical($"Variable '{VarName}' doesn't exist");
            }

            UpdateResultType();
        }

        protected override void DoUpdateResultType()
        {
            ResultType = ElementResultType.Create(LanguageSymbols.Instance.GetTypeByName(VarType));
        }

        private string m_varType;
    }

    public class FunctionCallElement : ValueElement
    {
        public override int NumberOfBranches
        {
            get { return CallArguments.Count; }
        }

        public override int RequiredSpaceInLocals
        {
            get { return m_returnTypesSize.Aggregate((l, r) => l + r); }
        }


        public LanguageFunction FunctionInfo
        {
            get { return m_langFunction; }
            set
            {
                m_langFunction = value;
                UpdateResultType();
            }
        }

        public List<ValueElement> CallArguments { get; set; }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            if (!m_isGenerateCalled)
            {
                foreach (ValueElement i in CallArguments)
                {
                    i.GenerateForBranch(generator, -1);
                }

                generator.AddOp(GenCodes.CallFunc, 2, ByteConverter
                    .New()
                    .CastInt32(generator.GetFunctionAddress(FunctionInfo.BuildName))
                    .CastInt32(
                        FunctionInfo.Arguments.Select(arg =>
                                LanguageSymbols
                                    .Instance
                                    .GetTypeSize(arg.TypeInfo.Name))
                            .Aggregate(0, (val1, val2) => val1 + val2)
                    )
                    .Bytes);

                int addressOffset = RequiredSpaceInLocals;

                foreach (var retTypeSize in m_returnTypesSize)
                {
                    addressOffset -= retTypeSize;
                    generator.AddOp(GenCodes.ConvertAddressSetTempVar, 1,
                        ByteConverter.New().CastInt32(addressOffset).CastInt32(retTypeSize).Bytes);
                }
            }

            m_isGenerateCalled = true;

            if (branch != -1 && FunctionInfo.ReturnTypes.Count > 0)
            {
                int offset = 0;
                for (int i = 0; i < branch; ++i)
                {
                    offset += m_returnTypesSize[i];
                }

                generator.AddOp(GenCodes.ConvertAddressGetTempVar, 2,
                    ByteConverter.New()
                        .CastInt32(offset)
                        .CastInt32(m_returnTypesSize[branch])
                        .Bytes);
            }
        }

        public override void PrepareBeforeGenerate()
        {
            CallArguments.ForEach(arg => arg.IsGet = true);

            UpdateResultType();
        }

        public override void CleanUpStatement(Generator generator)
        {
        }

        public override void GenerateInstructions(Generator generator)
        {
            GenerateForBranch(generator, -1);
        }

        protected override void DoUpdateResultType()
        {
            ResultType = ElementResultType.Create(FunctionInfo.ReturnTypes);
            m_returnTypesSize = (from retType in FunctionInfo.ReturnTypes
                select LanguageSymbols.Instance.GetTypeSize(retType.Name)).ToList();
        }

        private LanguageFunction m_langFunction;

        List<int> m_returnTypesSize;

        private bool m_isGenerateCalled;
    }

    public class MultipleValElement : ValueElement
    {
        public override int NumberOfBranches
        {
            get { return m_values.Count; }
        }

        public override int RequiredSpaceInLocals
        {
            get { return m_values.Max(val => val.RequiredSpaceInLocals); }
        }

        public void AddValue(ValueElement elem)
        {
            m_values.Add(elem);
            AddChild(elem);
        }

        public List<ValueElement> GetValues()
        {
            return m_values;
        }

        public override void PrepareBeforeGenerate()
        {
            m_values = Children<ValueElement>().ToList();
            UpdateResultType();
        }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            m_values[branch].GenerateForBranch(generator, 0);
        }

        public override void GenerateInstructions(Generator generator)
        {
            for (var index = 0; index < m_values.Count; index++)
            {
                GenerateForBranch(generator, index);
                ++index;
            }
        }

        protected override void DoUpdateResultType()
        {
            ResultType = ElementResultType.Create(m_values.Select(val => val.ResultType.ResultTypes[0]));
        }

        private List<ValueElement> m_values = new List<ValueElement>();
    }

    public class VarDeclarationElement : TreeElement
    {
        public string VarType;
        public string VarName;

        public bool IgnoreInitialization;
        public ValueElement InitVal;

        public override void PrepareBeforeGenerate()
        {
            if (IgnoreInitialization)
                return;

            InitVal.IsGet = true;
        }

        public override void GenerateInstructions(Generator generator)
        {
            if (IgnoreInitialization)
            {
                return;
            }

            InitVal.GenerateInstructions(generator);
            generator.AddOp(GenCodes.NewLVar, 2,
                ByteConverter.New()
                    .CastInt32(generator.GetLocalVarAddress(VarType, VarName))
                    .CastInt32(LanguageSymbols.Instance.GetTypeSize(VarType))
                    .Bytes);
        }

        public void GenLowLevelDelete(Generator generator)
        {
        }
    }

    public class MultipleVarDeclarationElement : TreeElement
    {
        public override void GenerateInstructions(Generator generator)
        {
            foreach (var i in Vars)
            {
                i.GenerateInstructions(generator);
            }
        }

        public void AddVar(VarDeclarationElement elem)
        {
            Vars.Add(elem);
            AddChild(elem);
        }

        public VarDeclarationElement GetVar(int i)
        {
            return Vars[i];
        }

        public List<VarDeclarationElement> GetVars()
        {
            return Vars; //TODO: AsReadOnly
        }

        private List<VarDeclarationElement> Vars = new List<VarDeclarationElement>();
    }
}