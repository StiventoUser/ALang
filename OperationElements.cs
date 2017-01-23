using System;
using System.Collections.Generic;
using System.Linq;

namespace ALang
{
    public static class OperationPriority
    {
        public static readonly Dictionary<string, int> Priorities = new Dictionary<string, int>
        {
            //string operation: 1)(1 symbol) b - binary, u - unary; 2)(1 or more symbols) operation
            {"b=", 3},
            {"b+", 5},
            {"b-", 5},
            {"b*", 6},
            {"b/", 6},
            {"b^", 7},
            {"u-", 8}
        };

        public static readonly Dictionary<string, bool> IsCompareToThisAsLessPriority = new Dictionary<string, bool>
        {
            //string operation: 1)(1 symbol) b - binary, u - unary; 2)(1 or more symbols) operation
            {"b=", false},
            {"b+", true},
            {"b-", true},
            {"b*", true},
            {"b/", true},
            {"b^", true},
            {"u-", true}
        };
    }

    public abstract class OperationElement : ValueElement
    {
        public abstract bool HasLeftOperand { get; }
        public abstract bool HasRightOperand { get; }
        public abstract string OperationName { get; }
        public abstract int Priority { get; }
    }

    public class BinaryPlusElement : OperationElement
    {
        public override int NumberOfBranches
        {
            get
            {
                if (ResultType == null)
                    UpdateResultType();
                return ResultType.ResultTypes.Count;
            }
        }

        public override int RequiredSpaceInLocals
        {
            get { return Child<ValueElement>(0).RequiredSpaceInLocals + Child<ValueElement>(1).RequiredSpaceInLocals; }
        }

        public override bool HasLeftOperand
        {
            get { return true; }
        }

        public override bool HasRightOperand
        {
            get { return true; }
        }

        public override string OperationName
        {
            get { return "b+"; }
        }

        public override int Priority
        {
            get { return OperationPriority.Priorities[OperationName]; }
        }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            var leftChild = Child<ValueElement>(0);
            var rightChild = Child<ValueElement>(1);

            leftChild.GenerateForBranch(generator, branch);
            rightChild.GenerateForBranch(generator, branch);

            generator.AddOp(GenCodes.Add, 1, ByteConverter
                .New()
                .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(ResultType.ResultTypes[branch].Name))
                .Bytes);
        }

        public override void GenerateInstructions(Generator generator)
        {
            for (int i = 0; i < NumberOfBranches; ++i)
            {
                GenerateForBranch(generator, i);
            }
        }

        public override void PrepareBeforeGenerate()
        {
            Child<ValueElement>(0).IsGet = true;
            Child<ValueElement>(1).IsGet = true;
            //TODO: operands count assert
            UpdateResultType();
        }

        protected override void DoUpdateResultType()
        {
            var leftChild = Child<ValueElement>(0);
            var rightChild = Child<ValueElement>(1);

            Compilation.Assert(leftChild.NumberOfBranches == rightChild.NumberOfBranches,
                "Values count at left of operation (" + leftChild.NumberOfBranches +
                ") must be equal count at right (" + rightChild.NumberOfBranches + ")", Line);

            ResultType = ElementResultType.Create
            (
                leftChild.ResultType.ResultTypes.Zip(rightChild.ResultType.ResultTypes,
                    (left, right) => { return LanguageSymbols.Instance.GetMostPrecise(left, right); }
                )
            );
        }
    }

    public class BinaryMinusElement : OperationElement
    {
        public override int NumberOfBranches
        {
            get
            {
                if (ResultType == null)
                    UpdateResultType();
                return ResultType.ResultTypes.Count;
            }
        }

        public override int RequiredSpaceInLocals
        {
            get { return Child<ValueElement>(0).RequiredSpaceInLocals + Child<ValueElement>(1).RequiredSpaceInLocals; }
        }

        public override bool HasLeftOperand
        {
            get { return true; }
        }

        public override bool HasRightOperand
        {
            get { return true; }
        }

        public override string OperationName
        {
            get { return "b-"; }
        }

        public override int Priority
        {
            get { return OperationPriority.Priorities[OperationName]; }
        }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            Child<ValueElement>(0).GenerateForBranch(generator, branch);
            Child<ValueElement>(1).GenerateForBranch(generator, branch);

            generator.AddOp(GenCodes.Subtract, 1, ByteConverter
                .New()
                .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(ResultType.ResultTypes[branch].Name))
                .Bytes);
        }

        public override void GenerateInstructions(Generator generator)
        {
            for (int i = 0; i < NumberOfBranches; ++i)
            {
                GenerateForBranch(generator, i);
            }
        }

        public override void PrepareBeforeGenerate()
        {
            Child<ValueElement>(0).IsGet = true;
            Child<ValueElement>(1).IsGet = true;
            //TODO: operands count assert
            UpdateResultType();
        }

        protected override void DoUpdateResultType()
        {
            var leftChild = Child<ValueElement>(0);
            var rightChild = Child<ValueElement>(1);

            Compilation.Assert(leftChild.NumberOfBranches == rightChild.NumberOfBranches,
                "Values count at left of operation (" + leftChild.NumberOfBranches +
                ") must be equal count at right (" + rightChild.NumberOfBranches + ")", Line);

            ResultType = ElementResultType.Create
            (
                leftChild.ResultType.ResultTypes.Zip(rightChild.ResultType.ResultTypes,
                    (left, right) => { return LanguageSymbols.Instance.GetMostPrecise(left, right); }
                )
            );
        }
    }

    public class BinaryMultiplicationElement : OperationElement
    {
        public override int NumberOfBranches
        {
            get
            {
                if (ResultType == null)
                    UpdateResultType();
                return ResultType.ResultTypes.Count;
            }
        }

        public override int RequiredSpaceInLocals
        {
            get { return Child<ValueElement>(0).RequiredSpaceInLocals + Child<ValueElement>(1).RequiredSpaceInLocals; }
        }

        public override bool HasLeftOperand
        {
            get { return true; }
        }

        public override bool HasRightOperand
        {
            get { return true; }
        }

        public override string OperationName
        {
            get { return "b*"; }
        }

        public override int Priority
        {
            get { return OperationPriority.Priorities[OperationName]; }
        }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            Child<ValueElement>(0).GenerateForBranch(generator, branch);
            Child<ValueElement>(1).GenerateForBranch(generator, branch);

            generator.AddOp(GenCodes.Multiply, 1, ByteConverter
                .New()
                .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(ResultType.ResultTypes[branch].Name))
                .Bytes);
        }

        public override void GenerateInstructions(Generator generator)
        {
            for (int i = 0; i < NumberOfBranches; ++i)
            {
                GenerateForBranch(generator, i);
            }
        }

        public override void PrepareBeforeGenerate()
        {
            Child<ValueElement>(0).IsGet = true;
            Child<ValueElement>(1).IsGet = true;
            //TODO: operands count assert
            UpdateResultType();
        }

        protected override void DoUpdateResultType()
        {
            var leftChild = Child<ValueElement>(0);
            var rightChild = Child<ValueElement>(1);

            Compilation.Assert(leftChild.NumberOfBranches == rightChild.NumberOfBranches,
                "Values count at left of operation (" + leftChild.NumberOfBranches +
                ") must be equal count at right (" + rightChild.NumberOfBranches + ")", Line);

            ResultType = ElementResultType.Create
            (
                leftChild.ResultType.ResultTypes.Zip(rightChild.ResultType.ResultTypes,
                    (left, right) => { return LanguageSymbols.Instance.GetMostPrecise(left, right); }
                )
            );
        }
    }

    public class BinaryDivisionElement : OperationElement
    {
        public override int NumberOfBranches
        {
            get
            {
                if (ResultType == null)
                    UpdateResultType();
                return ResultType.ResultTypes.Count;
            }
        }

        public override int RequiredSpaceInLocals
        {
            get { return Child<ValueElement>(0).RequiredSpaceInLocals + Child<ValueElement>(1).RequiredSpaceInLocals; }
        }

        public override bool HasLeftOperand
        {
            get { return true; }
        }

        public override bool HasRightOperand
        {
            get { return true; }
        }

        public override string OperationName
        {
            get { return "b/"; }
        }

        public override int Priority
        {
            get { return OperationPriority.Priorities[OperationName]; }
        }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            Child<ValueElement>(0).GenerateForBranch(generator, branch);
            Child<ValueElement>(1).GenerateForBranch(generator, branch);

            generator.AddOp(GenCodes.Divide, 1, ByteConverter
                .New()
                .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(ResultType.ResultTypes[branch].Name))
                .Bytes);
        }

        public override void GenerateInstructions(Generator generator)
        {
            for (int i = 0; i < NumberOfBranches; ++i)
            {
                GenerateForBranch(generator, i);
            }
        }

        public override void PrepareBeforeGenerate()
        {
            Child<ValueElement>(0).IsGet = true;
            Child<ValueElement>(1).IsGet = true;
            //TODO: operands count assert
            UpdateResultType();
        }

        protected override void DoUpdateResultType()
        {
            var leftChild = Child<ValueElement>(0);
            var rightChild = Child<ValueElement>(1);

            Compilation.Assert(leftChild.NumberOfBranches == rightChild.NumberOfBranches,
                "Values count at left of operation (" + leftChild.NumberOfBranches +
                ") must be equal count at right (" + rightChild.NumberOfBranches + ")", Line);

            ResultType = ElementResultType.Create
            (
                leftChild.ResultType.ResultTypes.Zip(rightChild.ResultType.ResultTypes,
                    (left, right) => { return LanguageSymbols.Instance.GetMostPrecise(left, right); }
                )
            );
        }
    }

    public class BinaryExponentiationElement : OperationElement
    {
        public override int NumberOfBranches
        {
            get
            {
                if (ResultType == null)
                    UpdateResultType();
                return ResultType.ResultTypes.Count;
            }
        }

        public override int RequiredSpaceInLocals
        {
            get { return Child<ValueElement>(0).RequiredSpaceInLocals + Child<ValueElement>(1).RequiredSpaceInLocals; }
        }

        public override bool HasLeftOperand
        {
            get { return true; }
        }

        public override bool HasRightOperand
        {
            get { return true; }
        }

        public override string OperationName
        {
            get { return "b^"; }
        }

        public override int Priority
        {
            get { return OperationPriority.Priorities[OperationName]; }
        }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            Child<ValueElement>(0).GenerateForBranch(generator, branch);
            Child<ValueElement>(1).GenerateForBranch(generator, branch);

            generator.AddOp(GenCodes.Exponent, 1, ByteConverter
                .New()
                .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(ResultType.ResultTypes[branch].Name))
                .Bytes);
        }

        public override void GenerateInstructions(Generator generator)
        {
            for (int i = 0; i < NumberOfBranches; ++i)
            {
                GenerateForBranch(generator, i);
            }
        }

        public override void PrepareBeforeGenerate()
        {
            Child<ValueElement>(0).IsGet = true;
            Child<ValueElement>(1).IsGet = true;
            //TODO: operands count assert
            UpdateResultType();
        }

        protected override void DoUpdateResultType()
        {
            var leftChild = Child<ValueElement>(0);
            var rightChild = Child<ValueElement>(1);

            Compilation.Assert(leftChild.NumberOfBranches == rightChild.NumberOfBranches,
                "Values count at left of operation (" + leftChild.NumberOfBranches +
                ") must be equal count at right (" + rightChild.NumberOfBranches + ")", Line);

            ResultType = ElementResultType.Create
            (
                leftChild.ResultType.ResultTypes.Zip(rightChild.ResultType.ResultTypes,
                    (left, right) => { return LanguageSymbols.Instance.GetMostPrecise(left, right); }
                )
            );
        }
    }

    public class UnaryMinusElement : OperationElement
    {
        public override int NumberOfBranches
        {
            get
            {
                if (ResultType == null)
                    UpdateResultType();
                return ResultType.ResultTypes.Count;
            }
        }

        public override int RequiredSpaceInLocals
        {
            get { return Child<ValueElement>(0).RequiredSpaceInLocals; }
        }

        public override bool HasLeftOperand
        {
            get { return false; }
        }

        public override bool HasRightOperand
        {
            get { return true; }
        }

        public override string OperationName
        {
            get { return "u-"; }
        }

        public override int Priority
        {
            get { return OperationPriority.Priorities[OperationName]; }
        }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            Child<ValueElement>(1).GenerateForBranch(generator, branch);

            generator.AddOp(GenCodes.Negate, 1, ByteConverter
                .New()
                .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(ResultType.ResultTypes[branch].Name))
                .Bytes);
        }

        public override void GenerateInstructions(Generator generator)
        {
            for (int i = 0; i < NumberOfBranches; ++i)
            {
                GenerateForBranch(generator, i);
            }
        }

        public override void PrepareBeforeGenerate()
        {
            Child<ValueElement>(1).IsGet = true;
            //TODO: operands count assert
            UpdateResultType();
        }

        protected override void DoUpdateResultType()
        {
            ResultType = ElementResultType.Create(Child<ValueElement>(1).ResultType.ResultTypes);
        }
    }

    public class CopyElement : OperationElement
    {
        public override int NumberOfBranches
        {
            get
            {
                if (ResultType == null)
                    UpdateResultType();
                return ResultType.ResultTypes.Count;
            }
        }

        public override int RequiredSpaceInLocals
        {
            get { return Child<ValueElement>(0).RequiredSpaceInLocals + Child<ValueElement>(1).RequiredSpaceInLocals; }
        }

        public override bool HasLeftOperand
        {
            get { return true; }
        }

        public override bool HasRightOperand
        {
            get { return true; }
        }

        public override string OperationName
        {
            get { return "b="; }
        }

        public override int Priority
        {
            get { return OperationPriority.Priorities[OperationName]; }
        }

        public void GenerateExistingResultForBranch(Generator generator, int branch)
        {
            MultipleValElement leftOperand = Child<MultipleValElement>(0);

            leftOperand.Child<GetVariableElement>(branch).ResultOfGeneration = GetVariableElement.GenerateOptions.Value;

            leftOperand.GenerateForBranch(generator, branch);
        }

        public override void GenerateForBranch(Generator generator, int branch)
        {
            Compilation.WriteCritical("BUG: CopyElement tried to generate a branch");
        }

        public override void GenerateInstructions(Generator generator)
        {
            ValueElement leftOperand = (ValueElement) Child(0);
            ValueElement rightOperand = (ValueElement) Child(1);

            foreach (var getVariableElement in leftOperand.Children<GetVariableElement>())
            {
                getVariableElement.ResultOfGeneration = GetVariableElement.GenerateOptions.Address;
            }

            if (!m_hasNestedCopyElement)
            {
                for (int i = 0, end = NumberOfBranches; i < end; ++i)
                {
                    leftOperand.GenerateForBranch(generator, i);
                    rightOperand.GenerateForBranch(generator, i);
                }
                for (int i = 0, end = NumberOfBranches; i < end; ++i)
                {
                    generator.AddOp(GenCodes.SetLVarVal, 1, ByteConverter
                        .New()
                        .CastInt32(LanguageSymbols
                            .Instance
                            .GetTypeSize(rightOperand
                                .ResultType
                                .ResultTypes[i]
                                .Name))
                        .Bytes);
                }
            }
            else
            {
                var nestedCopyElement = (CopyElement) rightOperand;
                nestedCopyElement.GenerateInstructions(generator);

                for (int i = 0, end = NumberOfBranches; i < end; ++i)
                {
                    leftOperand.GenerateForBranch(generator, i);
                    nestedCopyElement.GenerateExistingResultForBranch(generator, i);
                }
                for (int i = 0, end = NumberOfBranches; i < end; ++i)
                {
                    generator.AddOp(GenCodes.SetLVarVal, 1, ByteConverter
                        .New()
                        .CastInt32(LanguageSymbols
                            .Instance
                            .GetTypeSize(nestedCopyElement
                                .ResultType
                                .ResultTypes[i]
                            )
                        )
                        .Bytes);
                }
            }
        }

        public override void PrepareBeforeGenerate()
        {
            Child<ValueElement>(0).IsSet = true;
            Child<ValueElement>(1).IsGet = true;
            //TODO: operands count assert
            UpdateResultType();

            ValueElement leftOperand = (ValueElement) Child(0);
            ValueElement rightOperand = (ValueElement) Child(1);

            Compilation.Assert(leftOperand.NumberOfBranches == rightOperand.NumberOfBranches,
                "Each lvalue is assigned to only one rvalue", Line);

            m_hasNestedCopyElement = rightOperand is CopyElement;

            leftOperand.IsSet = true;
            rightOperand.IsGet = true;
        }

        protected override void DoUpdateResultType()
        {
            ResultType = ElementResultType.Create(Child<ValueElement>(0).ResultType.ResultTypes);
        }

        private bool m_hasNestedCopyElement;
    }
}