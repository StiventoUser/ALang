using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Stores built-in or custom type
/// </summary>
public sealed class LanguageType
{
    /// <summary>
    /// Type name
    /// </summary>
    public string Name;

    /// <summary>
    /// Is built-in
    /// </summary>
    public bool IsReserved;

    /// <summary>
    /// Is integer
    /// </summary>
    public bool IsInteger;

    /// <summary>
    /// Is floating point
    /// </summary>
    public bool IsFloat;
}

/// <summary>
/// Stores defined function
/// </summary>
public sealed class LanguageFunction
{
    /// <summary>
    /// Stores function argument
    /// </summary>
    public sealed class FunctionArg
    {
        /// <summary>
        /// Reference to argument type
        /// </summary>
        public LanguageType TypeInfo;

        /// <summary>
        /// Argument name
        /// </summary>
        public string ArgName;

        /// <summary>
        /// Argument default value. Can be null
        /// </summary>
        public ValueElement DefaultVal;
    }

    /// <summary>
    /// Function name
    /// </summary>
    public string Name;

    /// <summary>
    /// Function name with arguments types
    /// </summary>
    public string BuildName;

    /// <summary>
    /// Function arguments
    /// </summary>
    public List<FunctionArg> Arguments;

    /// <summary>
    /// Function return types
    /// </summary>
    public List<LanguageType> ReturnTypes;
}

/// <summary>
/// Stores convertion between 2 types
/// </summary>
public sealed class PossibleConvertions
{
    /// <summary>
    /// Source type
    /// </summary>
    public string FromType;

    /// <summary>
    /// Target type
    /// </summary>
    public string ToType;

    /// <summary>
    /// Is cast exist
    /// </summary>
    public bool CanCast;

    /// <summary>
    /// Warning which compiler should display. Can be null or empty
    /// </summary>
    public string WarningMessage;
}

/// <summary>
/// Stores all type, function, etc information
/// </summary>
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

    /// <summary>
    /// Returns symbols
    /// </summary>
    /// <returns></returns>
    public static LanguageSymbols Instance
    {
        get
        {
            return m_this; 
        }
    }

    /// <summary>
    /// Returns built-in types
    /// </summary>
    /// <returns></returns>
    public List<LanguageType> GetDefaultTypes()
    {
        return m_defaultTypes;
    }

    /// <summary>
    /// Checks is type is a build-in type or custom
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool IsTypeExist(string name)
    {
        return m_defaultTypes.Exists(type => type.Name == name) ||
               m_userTypes.Exists(type => type.Name == name);
    }

    /// <summary>
    /// Returns type info by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public LanguageType GetTypeByName(string name)
    {
        var result = m_defaultTypes.Find(type => type.Name == name);

        if(result == null)
        {
            result = m_userTypes.Find(type => type.Name == name);
        }

        Compilation.Assert(result != null, "Type '" + name + "' wasn't found", -1);

        return result;
    }

    public string GetTypeNameByCSharpTypeName(string typeName)
    {
        try
        {
            return m_cSharpTypeToLang[typeName];
        }
        catch(KeyNotFoundException)
        {
            Compilation.WriteCritical(string.Format("BUG: CSharp type '{0}' wasn't found", typeName));
            return null;
        }
    }

    /// <summary>
    /// Checks is type is built-in
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool IsTypeReserved(string name)
    {
        return m_defaultTypes.Exists(type => type.Name == name);
    }

    /// <summary>
    /// Adds custom type
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool AddUserType(string name)
    {
        if(IsTypeExist(name))
            return false;

        m_userTypes.Add(new LanguageType{ Name = name, IsReserved = false });

        return true;
    }

    /// <summary>
    /// Returns type size in bytes
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int GetTypeSize(string name)
    {
        int size;
        bool ok = m_typesSize.TryGetValue(name, out size);
        
        Compilation.Assert(ok, "Type '" + name + "' isn't exist", -1);

        return size;
    }

    /// <summary>
    /// Returns default type id
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public sbyte GetDefaultTypeId(string name)
    {
        int index = m_defaultTypes.FindIndex(type => type.Name == name);

        Compilation.Assert(index != -1, "Type '" + name + "' isn't exist", -1);

        return (sbyte)index;
    }

    /// <summary>
    /// Checks is function passed with name and arguments exist. Argument type can be null
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public bool IsFunctionExist(string name, IList<string> args)
    {
        return m_userFunctions.Exists( func => func.Name == name &&
                                        func.Arguments.CompareTo(args, (arg1, arg2) => arg1.TypeInfo.Name == arg2 ||
                                                                                       (String.IsNullOrEmpty(arg2) && arg1.DefaultVal != null)
                                                                ) );
    }

    /// <summary>
    /// Checks is function passed with name and arguments exist. This function doesn't expect a default argument(null)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public bool IsFunctionExist(string name, IList<LanguageFunction.FunctionArg> args)
    {
        return m_userFunctions.Exists( func => func.Name == name &&
                                        func.Arguments.CompareTo(args, 
                                            (arg1, arg2) => arg1.TypeInfo.Name == arg2.TypeInfo.Name ) );
    }

    /// <summary>
    /// Returns function by name and arguments type. Argument type can be null
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public LanguageFunction GetFunction(string name, IList<string> args)
    {
        var functions = m_userFunctions.Where( func => func.Name == name &&
                                        func.Arguments.CompareTo(args, (arg1, arg2) => arg1.TypeInfo.Name == arg2 || 
                                                                                       (String.IsNullOrEmpty(arg2) && arg1.DefaultVal != null)
                                                                ) ).ToList();

        Compilation.Assert(functions.Count != 0, "No function isn't founded with name '" + name + "' and arguments' types["
                                                 + args.MergeInString() + "]", -1);
        Compilation.Assert(functions.Count == 1, "More than one functions are founded with name + '" + name + "'"
                                                 + " and argumentss' types [" + args.MergeInString() + "] . It's a bug", -1);

        return functions.First();
    }

    /// <summary>
    /// Updates function information in symbols or add new function
    /// </summary>
    /// <param name="function"></param>
    public void UpdateFunction(LanguageFunction function)
    {
        m_userFunctions.RemoveAll( func => func.Name == function.Name &&
                                    func.Arguments.CompareTo(function.Arguments, 
                                        (arg1, arg2) => arg1.TypeInfo.Name == arg2.TypeInfo.Name ) );
                                             
        UpdateFunctionBuildName(function);
        m_userFunctions.Add(function);
    }

    /// <summary>
    /// Returns function build name by name and arguments
    /// </summary>
    /// <param name="function"></param>
    /// <returns></returns>
    public string GetFunctionBuildName(LanguageFunction function)
    {
        string argsStr = "null";
        if(function.Arguments.Count != 0)
        {
            argsStr = function.Arguments.Select(arg => arg.TypeInfo.Name)
                                        .Aggregate((id1, id2) => id1 + "@" + id2);
        }
        return string.Format("{0}_@{1}", function.Name, argsStr);
    }

    /// <summary>
    /// Updates function build name by name and arguments
    /// </summary>
    /// <param name="function"></param>
    public void UpdateFunctionBuildName(LanguageFunction function)
    {
        function.BuildName = GetFunctionBuildName(function);
    }

    /// <summary>
    /// Adds new function or returns false if it exists
    /// </summary>
    /// <param name="function"></param>
    /// <returns>True if function is added, otherwise false</returns>
    public bool AddUserFunction(LanguageFunction function)
    {
        if(IsFunctionExist(function.Name, function.Arguments))
        {
            return false;
        }

        UpdateFunctionBuildName(function);
        
        m_userFunctions.Add(function);

        return true;
    }

    /// <summary>
    /// Adds new function or returns false if it exists
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="returnTypes"></param>
    /// <returns>True if function is added, otherwise false</returns>
    public bool AddUserFunction(string name, IList<LanguageFunction.FunctionArg> args, IList<string> returnTypes)
    {
        var typesInfo = returnTypes.Select(typeName => GetTypeByName(typeName));

        return AddUserFunction(new LanguageFunction{ Name = name, Arguments = args.ToList(), 
                                        ReturnTypes = typesInfo.ToList() });
    }

    /// <summary>
    /// Returns convertion information
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public PossibleConvertions GetCastInfo(string from, string to)
    {
        return m_defaultConvertions.Find(convertion => convertion.FromType == from && convertion.ToType == to );
    }

    /// <summary>
    /// Returns most precise type from passed
    /// </summary>
    /// <param name="types"></param>
    /// <returns></returns>
    public string GetMostPrecise(List<string> types)
    {
        bool allSupported = types.All(type => m_preciseLevel.Contains(type));
        if(!allSupported)
        {
            Compilation.WriteError("Type(s) [" + types.Where(type => !m_preciseLevel.Contains(type))
                                                      .Aggregate((type1, type2) => type1 + ", " + type2)
                                    + "] do(es) not support precission", -1);
        }
        return types.OrderBy(type => m_preciseLevel.IndexOf(type)).First();
    }

    /// <summary>
    /// Returns most precise type from passed
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public string GetMostPrecise(string first, string second)
    {
        return GetMostPrecise(new List<string>{first, second});
    }

    /// <summary>
    /// Returns type of built-in constant
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public string GetTypeOfConstVal(string val)
    {
        if(val.Contains('.'))
        {
            return DefTypesName.Get(DefTypesName.Index.Single);
        }
        else
        {
            return DefTypesName.Get(DefTypesName.Index.Int32);
        }
    }

    /// <summary>
    /// Get default value of type(null for custom)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Get default value of built-in type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public ConstValElement GetDefaultValOfDefaultType(string type)
    {
        return m_defaultValOfDefaultTypes[type];
    }

    public static class DefTypesName
    {
        public enum Index : byte
        {
            Int64, Int32, Int16, Int8, Double, Single, String, Bool 
        }

        public static string Get(Index index)
        {
            return m_defaultTypesName[(int)index];
        }

        private static string[] m_defaultTypesName = 
        { "Int64", "Int32", "Int16", "Int8", "Double", "Single", "String", "Bool" };
    }

    private Dictionary<string, ConstValElement> m_defaultValOfDefaultTypes = new Dictionary<string, ConstValElement>
    {
        { DefTypesName.Get(DefTypesName.Index.Int32), 
                           new ConstValElement{ Type = DefTypesName.Get(DefTypesName.Index.Int32), Value = "0" } }
    };

    private Dictionary<string, int> m_typesSize = new Dictionary<string, int>
    {
        { DefTypesName.Get(DefTypesName.Index.Int32), 4 },
        { DefTypesName.Get(DefTypesName.Index.Int8), 1 }
    };

    private List<string> m_preciseLevel = new List<string>
    {
        DefTypesName.Get(DefTypesName.Index.String),
        DefTypesName.Get(DefTypesName.Index.Double),
        DefTypesName.Get(DefTypesName.Index.Single),
        DefTypesName.Get(DefTypesName.Index.Int64),
        DefTypesName.Get(DefTypesName.Index.Int32),
        DefTypesName.Get(DefTypesName.Index.Int16),
        DefTypesName.Get(DefTypesName.Index.Int8),
        DefTypesName.Get(DefTypesName.Index.Bool)
    };

    private List<PossibleConvertions> m_defaultConvertions = new List<PossibleConvertions>
    {
        new PossibleConvertions{ FromType = DefTypesName.Get(DefTypesName.Index.Int8),
                                 ToType   = DefTypesName.Get(DefTypesName.Index.Int16),
                                 CanCast  = true }
    };

    
    //The order of this list !MUST! be same as order in DefTypesName
    private List<LanguageType> m_defaultTypes = new List<LanguageType>{ 
        new LanguageType{ Name = DefTypesName.Get(DefTypesName.Index.Int64), IsReserved = true, IsInteger = true },
        new LanguageType{ Name = DefTypesName.Get(DefTypesName.Index.Int32), IsReserved = true, IsInteger = true },
        new LanguageType{ Name = DefTypesName.Get(DefTypesName.Index.Int16), IsReserved = true, IsInteger = true },
        new LanguageType{ Name = DefTypesName.Get(DefTypesName.Index.Int8), IsReserved = true, IsInteger = true },
        new LanguageType{ Name = DefTypesName.Get(DefTypesName.Index.Double), IsReserved = true, IsFloat = true },
        new LanguageType{ Name = DefTypesName.Get(DefTypesName.Index.Single), IsReserved = true, IsFloat = true },
        new LanguageType{ Name = DefTypesName.Get(DefTypesName.Index.String), IsReserved = true },
        new LanguageType{ Name = DefTypesName.Get(DefTypesName.Index.Bool), IsReserved = true }
        };
    private List<LanguageType> m_userTypes = new List<LanguageType>();

    private List<LanguageFunction> m_userFunctions = new List<LanguageFunction>();

    private Dictionary<string, string> m_cSharpTypeToLang = new Dictionary<string, string>()
    {
        { typeof(SByte).Name, DefTypesName.Get(DefTypesName.Index.Int8) },
        { typeof(Int16).Name, DefTypesName.Get(DefTypesName.Index.Int16) },
        { typeof(Int32).Name, DefTypesName.Get(DefTypesName.Index.Int32) },
        { typeof(Int64).Name, DefTypesName.Get(DefTypesName.Index.Int64) },
        { typeof(Single).Name, DefTypesName.Get(DefTypesName.Index.Single) },
        { typeof(Double).Name, DefTypesName.Get(DefTypesName.Index.Double) },
        { typeof(String).Name, DefTypesName.Get(DefTypesName.Index.String) },
        { typeof(Boolean).Name, DefTypesName.Get(DefTypesName.Index.Bool) }
    };

    private static LanguageSymbols m_this; 
}