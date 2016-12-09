using System;
using System.Collections.Generic;
using System.Linq;

public static class OperationPriority
{
    public static readonly Dictionary<string, int> Priorities = new Dictionary<string, int>
    {
        //string operation: 1)(1 symbol) b - binary, u - unary; 2)(1 or more symbols) operation
        { "b=", 3 },
        { "b+", 5 },
        { "b-", 5 },
        { "b*", 6 },
        { "b/", 6 },
        { "b^", 7 },
        { "u-", 8 }
    };
    public static readonly Dictionary<string, bool> IsCompareToThisAsLessPriority = new Dictionary<string, bool>
    {
        //string operation: 1)(1 symbol) b - binary, u - unary; 2)(1 or more symbols) operation
        { "b=", false },
        { "b+", true },
        { "b-", true },
        { "b*", true },
        { "b/", true },
        { "b^", true },
        { "u-", true }
    };
}

public static class OperationHelper
{
    public static ElementResultTypePart GetMostPrecise(ElementResultTypePart first, ElementResultTypePart second)
    {
        string mostPreciseName = LanguageSymbols.Instance.GetMostPrecise(first.ResultType, second.ResultType);

        return first.ResultType == mostPreciseName ? first : second;
    }
}

public abstract class OperationElement : ValueElement
{
    public abstract bool HasLeftOperand{ get; }
    public abstract bool HasRightOperand{ get; }
    public abstract string OperationName{ get; }
    public abstract int Priority{ get; }
}

public class BinaryPlusElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            if(Result == null)
                UpdateResult();
            return Result.ResultTypes.Count;
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    
    public override string OperationName{ get{ return "b+"; } }
    public override int Priority{ get{ return OperationPriority.Priorities[OperationName]; } }

    public override void GenerateValue(Generator generator, int index)
    {
        Child<ValueElement>(0).GenerateValue(generator, index);
        Child<ValueElement>(1).GenerateValue(generator, index);

        generator.AddOp(GenCodes.Add, 1, ByteConverter
                                            .New()
                                            .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(
                                                Result.ResultTypes[index].ResultType
                                            ))
                                            .Bytes);
    }
    public override void GenLowLevel(Generator generator)
    { 
        for(int i = 0, end = ValCount; i < end; ++i)
        {
            GenerateValue(generator, i);
        }
    }

    public override void PrepareBeforeGenerate()
    {
        Child<ValueElement>(0).IsGet = true;
        Child<ValueElement>(1).IsGet = true;
        //TODO: operands count assert
        UpdateResult();
    }
    protected override void UpdateResultValue()
    {
        var leftChild = Child<ValueElement>(0);
        var rightChild = Child<ValueElement>(1);

        Compilation.Assert(leftChild.ValCount == rightChild.ValCount, 
                            "Values count at left of operation (" + leftChild.ValCount +
                            ") must be equal count at right (" + rightChild.ValCount + ")", Line);

        Result = ElementResultType.Create
        (
            leftChild.Result.ResultTypes.Zip(rightChild.Result.ResultTypes, (left, right) => 
            {
                return OperationHelper.GetMostPrecise(left, right);
            }
            )
        );
    }
}
public class BinaryMinusElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            if(Result == null)
                UpdateResult();
            return Result.ResultTypes.Count;
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "b-"; } }
    public override int Priority{ get{ return OperationPriority.Priorities[OperationName]; } }

    public override void GenerateValue(Generator generator, int index)
    {
        Child<ValueElement>(0).GenerateValue(generator, index);
        Child<ValueElement>(1).GenerateValue(generator, index);

        generator.AddOp(GenCodes.Subtract, 1, ByteConverter
                                            .New()
                                            .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(
                                                Result.ResultTypes[index].ResultType
                                            ))
                                            .Bytes);
    }
    public override void GenLowLevel(Generator generator)
    { 
        for(int i = 0, end = ValCount; i < end; ++i)
        {
            GenerateValue(generator, i);
        }
    }

    public override void PrepareBeforeGenerate()
    {
        Child<ValueElement>(0).IsGet = true;
        Child<ValueElement>(1).IsGet = true;
        //TODO: operands count assert
        UpdateResult();
    }
    protected override void UpdateResultValue()
    {
        var leftChild = Child<ValueElement>(0);
        var rightChild = Child<ValueElement>(1);

        Compilation.Assert(leftChild.ValCount == rightChild.ValCount, 
                            "Values count at left of operation (" + leftChild.ValCount +
                            ") must be equal count at right (" + rightChild.ValCount + ")", Line);

        Result = ElementResultType.Create
        (
            leftChild.Result.ResultTypes.Zip(rightChild.Result.ResultTypes, (left, right) => 
            {
                return OperationHelper.GetMostPrecise(left, right);
            }
            )
        );
    }
}
public class BinaryMultiplicationElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            if(Result == null)
                UpdateResult();
            return Result.ResultTypes.Count;
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "b*"; } }
    public override int Priority{ get{ return OperationPriority.Priorities[OperationName]; } }

    public override void GenerateValue(Generator generator, int index)
    {
        Child<ValueElement>(0).GenerateValue(generator, index);
        Child<ValueElement>(1).GenerateValue(generator, index);

        generator.AddOp(GenCodes.Multiply, 1, ByteConverter
                                            .New()
                                            .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(
                                                Result.ResultTypes[index].ResultType
                                            ))
                                            .Bytes);
    }
    public override void GenLowLevel(Generator generator)
    { 
        for(int i = 0, end = ValCount; i < end; ++i)
        {
            GenerateValue(generator, i);
        }
    }

    public override void PrepareBeforeGenerate()
    {
        Child<ValueElement>(0).IsGet = true;
        Child<ValueElement>(1).IsGet = true;
        //TODO: operands count assert
        UpdateResult();
    }
    protected override void UpdateResultValue()
    {
        var leftChild = Child<ValueElement>(0);
        var rightChild = Child<ValueElement>(1);

        Compilation.Assert(leftChild.ValCount == rightChild.ValCount, 
                            "Values count at left of operation (" + leftChild.ValCount +
                            ") must be equal count at right (" + rightChild.ValCount + ")", Line);

        Result = ElementResultType.Create
        (
            leftChild.Result.ResultTypes.Zip(rightChild.Result.ResultTypes, (left, right) => 
            {
                return OperationHelper.GetMostPrecise(left, right);
            }
            )
        );
    }
}
public class BinaryDivisionElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            if(Result == null)
                UpdateResult();
            return Result.ResultTypes.Count;
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "b/"; } }
    public override int Priority{ get{ return OperationPriority.Priorities[OperationName]; } }

    public override void GenerateValue(Generator generator, int index)
    {
        Child<ValueElement>(0).GenerateValue(generator, index);
        Child<ValueElement>(1).GenerateValue(generator, index);

        generator.AddOp(GenCodes.Divide, 1, ByteConverter
                                            .New()
                                            .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(
                                                Result.ResultTypes[index].ResultType
                                            ))
                                            .Bytes);
    }
    public override void GenLowLevel(Generator generator)
    { 
        for(int i = 0, end = ValCount; i < end; ++i)
        {
            GenerateValue(generator, i);
        }
    }

    public override void PrepareBeforeGenerate()
    {
        Child<ValueElement>(0).IsGet = true;
        Child<ValueElement>(1).IsGet = true;
        //TODO: operands count assert
        UpdateResult();
    }
    protected override void UpdateResultValue()
    {
        var leftChild = Child<ValueElement>(0);
        var rightChild = Child<ValueElement>(1);

        Compilation.Assert(leftChild.ValCount == rightChild.ValCount, 
                            "Values count at left of operation (" + leftChild.ValCount +
                            ") must be equal count at right (" + rightChild.ValCount + ")", Line);

        Result = ElementResultType.Create
        (
            leftChild.Result.ResultTypes.Zip(rightChild.Result.ResultTypes, (left, right) => 
            {
                return OperationHelper.GetMostPrecise(left, right);
            }
            )
        );
    }
}
public class BinaryExponentiationElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            if(Result == null)
                UpdateResult();
            return Result.ResultTypes.Count;
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "b^"; } }
    public override int Priority{ get{ return OperationPriority.Priorities[OperationName]; } }

    public override void GenerateValue(Generator generator, int index)
    {
        Child<ValueElement>(0).GenerateValue(generator, index);
        Child<ValueElement>(1).GenerateValue(generator, index);

        generator.AddOp(GenCodes.Exponent, 1, ByteConverter
                                            .New()
                                            .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(
                                                Result.ResultTypes[index].ResultType
                                            ))
                                            .Bytes);
    }
    public override void GenLowLevel(Generator generator)
    { 
        for(int i = 0, end = ValCount; i < end; ++i)
        {
            GenerateValue(generator, i);
        }
    }

    public override void PrepareBeforeGenerate()
    {
        Child<ValueElement>(0).IsGet = true;
        Child<ValueElement>(1).IsGet = true;
        //TODO: operands count assert
        UpdateResult();
    }
    protected override void UpdateResultValue()
    {
        var leftChild = Child<ValueElement>(0);
        var rightChild = Child<ValueElement>(1);

        Compilation.Assert(leftChild.ValCount == rightChild.ValCount, 
                            "Values count at left of operation (" + leftChild.ValCount +
                            ") must be equal count at right (" + rightChild.ValCount + ")", Line);

        Result = ElementResultType.Create
        (
            leftChild.Result.ResultTypes.Zip(rightChild.Result.ResultTypes, (left, right) => 
            {
                return OperationHelper.GetMostPrecise(left, right);
            }
            )
        );
    }
}
public class UnaryMinusElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            if(Result == null)
                UpdateResult();
            return Result.ResultTypes.Count;
        }
    }
    public override bool HasLeftOperand{ get{ return false; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "u-"; } }
    public override int Priority{ get{ return OperationPriority.Priorities[OperationName]; } }
           
    public override void GenerateValue(Generator generator, int index)
    {
        Child<ValueElement>(1).GenerateValue(generator, index);

        generator.AddOp(GenCodes.Negate, 1, ByteConverter
                                            .New()
                                            .CastByte(LanguageSymbols.Instance.GetDefaultTypeId(
                                                Result.ResultTypes[index].ResultType
                                            ))
                                            .Bytes);
    }
    public override void GenLowLevel(Generator generator)
    { 
        for(int i = 0, end = ValCount; i < end; ++i)
        {
            GenerateValue(generator, i);
        }
    }

    public override void PrepareBeforeGenerate()
    {
        Child<ValueElement>(1).IsGet = true;
        //TODO: operands count assert
        UpdateResult();
    }
    protected override void UpdateResultValue()
    {
        Result = ElementResultType.Create(Child<ValueElement>(1).Result.ResultTypes);
    }
}

public class CopyElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            if(Result == null)
                UpdateResult();
            return Result.ResultTypes.Count; 
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "b="; } }
    public override int Priority{ get{ return OperationPriority.Priorities[OperationName]; } }

    public override void GenerateValue(Generator generator, int index)
    {
        ValueElement leftOperand = (ValueElement)Child(0);
        ValueElement rightOperand = (ValueElement)Child(1);

        Compilation.Assert(leftOperand.ValCount == rightOperand.ValCount,
                           "Each lvalue is assigned to only one rvalue", Line); 

        rightOperand.GenerateValue(generator, index);
        leftOperand.GenerateValue(generator, index);
    }
    public override void GenLowLevel(Generator generator)
    { 
        for(int i = 0, end = ValCount; i < end; ++i)
        {
            GenerateValue(generator, i);
        }
    }

    public override void PrepareBeforeGenerate()
    {
        Child<ValueElement>(0).IsSet = true;
        Child<ValueElement>(1).IsGet = true;
        //TODO: operands count assert
        UpdateResult();
    }
    protected override void UpdateResultValue()
    {
        Result = ElementResultType.Create(Child<ValueElement>(0).Result.ResultTypes);
    }
} 