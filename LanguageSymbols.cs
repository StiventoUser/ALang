using System;
using System.Linq;
using System.Collections.Generic;

//TODO Refactoring

public sealed class LanguageType
{
    public string Name;
    public bool IsReserved;

    public bool IsInteger;
    public bool IsFloat;
}
public sealed class LanguageFunction
{
    public sealed class FunctionArg
    {
        public string TypeName;
        public string ArgName;
        public ValElement DefaultVal;
    }
    public string Name;

    public List<FunctionArg> Arguments;
    public List<string> ReturnTypes;
}
public sealed class PossibleConvertions
{
    public string FromType;
    public string ToType;

    public bool CanCast;
    public string WarningMessage;
}
public sealed class LanguageSymbols
{
    public LanguageSymbols()
    {
        if(m_this != null)
        {
            Compilation.WriteCritical("Language symbols have been already created");
        }
        m_this = this;
    }

    public static LanguageSymbols Instance
    {
        get
        {
            if(m_this != null)
            {
                Compilation.WriteCritical("Language symbols aren't exist");
            }
            return m_this; 
        }
    }

    public List<LanguageType> GetDefaultTypes()
    {
        return m_defaultTypes;
    }
    public bool IsTypeExist(string name)
    {
        return m_defaultTypes.Exists(type => type.Name == name) ||
               m_userTypes.Exists(type => type.Name == name);
    }
    public bool IsTypeReserved(string name)
    {
        return m_defaultTypes.Exists(type => type.Name == name);
    }

    public bool AddUserType(string name)
    {
        if(IsTypeExist(name))
            return false;

        m_userTypes.Add(new LanguageType{ Name = name, IsReserved = false });

        return true;
    }
    public bool IsFunctionExist(string name, IList<string> args)
    {
        return m_userFunctions.Exists( func => func.Name == name &&
                                               func.Arguments.CompareTo(args, (arg1, arg2) => arg1.TypeName == arg2 ) );
    }
    public bool IsFunctionExist(string name, IList<LanguageFunction.FunctionArg> args)
    {
        return m_userFunctions.Exists( func => func.Name == name &&
                                               func.Arguments.CompareTo(args, (arg1, arg2) => arg1.TypeName == arg2.TypeName ) );
    }
    public void UpdateFunction(LanguageFunction function)
    {
        m_userFunctions.RemoveAll( func => func.Name == function.Name &&
                                func.Arguments.CompareTo(function.Arguments, 
                                    (arg1, arg2) => arg1.TypeName == arg2.TypeName ) );
                                             
        m_userFunctions.Add(function);
    }
    public bool AddUserFunction(LanguageFunction function)
    {
        if(IsFunctionExist(function.Name, function.Arguments))
        {
            return false;
        }

        m_userFunctions.Add(function);

        return true;
    }
    public bool AddUserFunction(string name, IList<LanguageFunction.FunctionArg> args, IList<string> returnTypes)
    {
        return AddUserFunction(new LanguageFunction{ Name = name, Arguments = args.ToList(), ReturnTypes = returnTypes.ToList() });
    }

    public PossibleConvertions GetCastInfo(string from, string to)
    {
        return m_defaultConvertions.Find(convertion => convertion.FromType == from && convertion.ToType == to );
    }
    public string GetMostPrecise(List<string> types)
    {
        var unsupported = types.Where(type => !m_preciseLevel.Contains(type));
        if(unsupported.Any())
        {
            Compilation.WriteError("Type(s) [" + unsupported.Aggregate((type1, type2) => type1 + ", " + type2)
                                    + "] do(es) not support precission", -1);
        }
        return types.OrderBy(type => m_preciseLevel.IndexOf(type)).First();
    }

    public string GetTypeOfConstVal(string val)
    {
        if(val.Contains('.'))
        {
            return "single";
        }
        else
        {
            return "int32";
        }
    }
    public ConstValElement GetDefaultVal(string type)
    {
        if(m_defaultValOfDefaultTypes.ContainsKey(type))
        {
            return GetDefaultValOfDefaultType(type);
        }
        else
        {
            return new ConstValElement{ Type = type, Value = "null" };
        }
    }
    public ConstValElement GetDefaultValOfDefaultType(string type)
    {
        return m_defaultValOfDefaultTypes[type];
    }

    private Dictionary<string, ConstValElement> m_defaultValOfDefaultTypes = new Dictionary<string, ConstValElement>
    {
        { "int32", new ConstValElement{ Type = "int32", Value = "0" } }
    };
    private List<string> m_preciseLevel = new List<string>
    {
        "string", "double", "single", "int64", "int32", "int16", "int8", "bool"
    };

    private List<PossibleConvertions> m_defaultConvertions = new List<PossibleConvertions>
    {
        new PossibleConvertions{ FromType = "int8", ToType = "int16", CanCast = true }
    };

    private List<LanguageType> m_defaultTypes = new List<LanguageType>{ 
        new LanguageType{ Name = "int8", IsReserved = true, IsInteger = true },
        new LanguageType{ Name = "int16", IsReserved = true, IsInteger = true },
        new LanguageType{ Name = "int32", IsReserved = true, IsInteger = true },
        new LanguageType{ Name = "int64", IsReserved = true, IsInteger = true },
        new LanguageType{ Name = "bool", IsReserved = true },
        new LanguageType{ Name = "single", IsReserved = true, IsFloat = true },
        new LanguageType{ Name = "double", IsReserved = true, IsFloat = true },
        new LanguageType{ Name = "string", IsReserved = true }
        };
    private List<LanguageType> m_userTypes = new List<LanguageType>();

    private List<LanguageFunction> m_userFunctions = new List<LanguageFunction>();

    private static LanguageSymbols m_this; 
}