using System;
using System.Collections.Generic;

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
}

public abstract class OperationElement : ValElement
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
            return Math.Max(Child<ValElement>(0).ValCount, Child<ValElement>(1).ValCount);  
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    
    public override string OperationName{ get{ return "binary+"; } }
    public override int Priority{ get{ return OperationPriority.Priorities["b+"]; } }
    /*private ElementResultType GetResultOf()
    {
        var result1 = Value1.Result;
        var result2 = Value2.Result;

        if(result1.IsFunction || result2.IsFunction)
        {
            Compilation.WriteError("Operator '+': function can't be used as operand", Line);
        }

        string fromType;
        string resultType;

        if(Value1.Result != Value2.Result)
        {
            resultType = LanguageSymbols.Instance.GetMostPrecise(new List<string>{result1.ResultType, result2.ResultType});

            fromType = result1.ResultType == resultType ? result2.ResultType : result1.ResultType;
        }
        else
        {
            fromType = resultType = result1.ResultType;
        }

        var castInfo = LanguageSymbols.Instance.GetCastInfo(fromType, resultType);

        if(castInfo == null)
        {
            Compilation.WriteError("No cast info: from '" + fromType + "' to '" + resultType + "'.", Line);
        }
        if(castInfo.CanCast)
        {
            if(!String.IsNullOrEmpty(castInfo.WarningMessage))
            {
                Compilation.WriteWarning("Cast warning: '" + castInfo.WarningMessage + "'.", Line);
            }

            return new ElementResultType{ IsFunction = false, ResultType = resultType }; 
        }
        else
        {
            Compilation.WriteError("Cast forbidden: from '" + fromType + "' to '" + resultType + "'.", Line);
            return null;//only for compiler
        }
    }*/

    public override void GenLowLevel(Generator generator)
    {
        m_children[0].GenLowLevel(generator);
        m_children[1].GenLowLevel(generator);

        generator.AddOp(GenCodes.Add, 0, null);
    }
}
public class BinaryMinusElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            return Math.Max(Child<ValElement>(0).ValCount, Child<ValElement>(1).ValCount);  
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "binary-"; } }
    public override int Priority{ get{ return OperationPriority.Priorities["b-"]; } }

    public override void GenLowLevel(Generator generator)
    {
        m_children[0].GenLowLevel(generator);
        m_children[1].GenLowLevel(generator);

        generator.AddOp(GenCodes.Subtract, 0, null);
    }
}
public class BinaryMultiplicationElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            return Math.Max(Child<ValElement>(0).ValCount, Child<ValElement>(1).ValCount);  
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "binary*"; } }
    public override int Priority{ get{ return OperationPriority.Priorities["b*"]; } }

    public override void GenLowLevel(Generator generator)
    {
        m_children[0].GenLowLevel(generator);
        m_children[1].GenLowLevel(generator);

        generator.AddOp(GenCodes.Multiply, 0, null);
    }
}
public class BinaryDivisionElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            return Math.Max(Child<ValElement>(0).ValCount, Child<ValElement>(1).ValCount);  
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "binary/"; } }
    public override int Priority{ get{ return OperationPriority.Priorities["b/"]; } }

    public override void GenLowLevel(Generator generator)
    {
        m_children[0].GenLowLevel(generator);
        m_children[1].GenLowLevel(generator);

        generator.AddOp(GenCodes.Divide, 0, null);
    }
}
public class BinaryExponentiationElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            return Math.Max(Child<ValElement>(0).ValCount, Child<ValElement>(1).ValCount);  
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "binary^"; } }
    public override int Priority{ get{ return OperationPriority.Priorities["b^"]; } }

    public override void GenLowLevel(Generator generator)
    {
        m_children[0].GenLowLevel(generator);
        m_children[1].GenLowLevel(generator);

        generator.AddOp(GenCodes.Exponent, 0, null);
    }
}
public class UnaryMinusElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            return Child<ValElement>(1).ValCount;  
        }
    }
    public override bool HasLeftOperand{ get{ return false; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "unary-"; } }
    public override int Priority{ get{ return OperationPriority.Priorities["u-"]; } }

    public override void GenLowLevel(Generator generator)
    {
        m_children[1].GenLowLevel(generator);

        generator.AddOp(GenCodes.Negate, 0, null);
    }
}

public class CopyElement : OperationElement
{
    public override int ValCount
    {
        get
        {
            return Math.Max(Child<ValElement>(0).ValCount, Child<ValElement>(1).ValCount);  
        }
    }
    public override bool HasLeftOperand{ get{ return true; } }
    public override bool HasRightOperand{ get{ return true; } }
    public override string OperationName{ get{ return "binary="; } }
    public override int Priority{ get{ return OperationPriority.Priorities["b="]; } }

    public override void GenLowLevel(Generator generator)
    {
        VarGetSetValElement LeftOperand = (VarGetSetValElement)Child(0);
        ValElement RightOperand = (ValElement)Child(1);

        LeftOperand.IsGet = true;
        LeftOperand.IsSet = true;
 
        RightOperand.GenLowLevel(generator);
        LeftOperand.GenLowLevel(generator);
    }
}