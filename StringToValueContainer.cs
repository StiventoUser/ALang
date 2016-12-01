using System;

/// <summary>
/// Contains constant value as a string. To get a value use GetValue<Type>() 
/// </summary>
public sealed class StringToValue
{
    public string TypeName
    {
        get{ return m_typeName; }
        set
        {
            m_typeName = value;
            m_typeId = LanguageSymbols.Instance.GetDefaultTypeId(m_typeName);
        }
    }
    public string StringVal{get;set;}

    public SByte GetInt8()
    {
        CheckValue(typeof(SByte).Name);

        try
        {
            return SByte.Parse(StringVal);
        }
        catch
        {
            Compilation.WriteError(string.Format("Can't parse '{0}' of type '{1}' to type '{2}'",
                                                 StringVal, TypeName, "Int8"), -1);
            throw new Exception();//This code only for compiler. Exception is generated at Compilation.WriteError
        }
    }
    public Int16 GetInt16()
    {
        CheckValue(typeof(Int16).Name);

        try
        {
            return Int16.Parse(StringVal);
        }
        catch
        {
            Compilation.WriteError(string.Format("Can't parse '{0}' of type '{1}' to type '{2}'",
                                                 StringVal, TypeName, "Int16"), -1);
            throw new Exception();//This code only for compiler. Exception is generated at Compilation.WriteError
        }
    }
    public Int32 GetInt32()
    {
        CheckValue(typeof(Int32).Name);

        try
        {
            return Int32.Parse(StringVal);
        }
        catch
        {
            Compilation.WriteError(string.Format("Can't parse '{0}' of type '{1}' to type '{2}'",
                                                 StringVal, TypeName, "Int32"), -1);
            throw new Exception();//This code only for compiler. Exception is generated at Compilation.WriteError
        }
    }
    public Int64 GetInt64()
    {
        CheckValue(typeof(Int64).Name);

        try
        {
            return Int64.Parse(StringVal);
        }
        catch
        {
            Compilation.WriteError(string.Format("Can't parse '{0}' of type '{1}' to type '{2}'",
                                                 StringVal, TypeName, "Int32"), -1);
            throw new Exception();//This code only for compiler. Exception is generated at Compilation.WriteError
        }
    }
    public Double GetDouble()
    {
        CheckValue(typeof(Double).Name);

        try
        {
            return Double.Parse(StringVal);
        }
        catch
        {
            Compilation.WriteError(string.Format("Can't parse '{0}' of type '{1}' to type '{2}'",
                                                 StringVal, TypeName, "Double"), -1);
            throw new Exception();//This code only for compiler. Exception is generated at Compilation.WriteError
        }
    }
    public Single GetSingle()
    {
        CheckValue(typeof(Single).Name);

        try
        {
            return Single.Parse(StringVal);
        }
        catch
        {
            Compilation.WriteError(string.Format("Can't parse '{0}' of type '{1}' to type '{2}'",
                                                 StringVal, TypeName, "Single"), -1);
            throw new Exception();//This code only for compiler. Exception is generated at Compilation.WriteError
        }
    }
    public String GetString()
    {
        CheckValue(typeof(String).Name);

        return StringVal;
    }
    public Boolean GetBoolean()
    {
        CheckValue(typeof(Boolean).Name);

        try
        {
            return Boolean.Parse(StringVal);
        }
        catch
        {
            Compilation.WriteError(string.Format("Can't parse '{0}' of type '{1}' to type '{2}'",
                                                 StringVal, TypeName, "Boolean"), -1);
            throw new Exception();//This code only for compiler. Exception is generated at Compilation.WriteError
        }
    }

    public void WriteBytes(ByteConverter converter)
    {
        switch((LanguageSymbols.DefTypesName.Index)m_typeId)
        {
            case LanguageSymbols.DefTypesName.Index.Int8:
                converter.CastByte(GetInt8()); 
                break;
            case LanguageSymbols.DefTypesName.Index.Int16:
                converter.CastInt16(GetInt16());
                break;
            case LanguageSymbols.DefTypesName.Index.Int32:
                converter.CastInt32(GetInt32());
                break;
            case LanguageSymbols.DefTypesName.Index.Int64:
                converter.CastInt64(GetInt64());
                break;
            case LanguageSymbols.DefTypesName.Index.Double:
                converter.CastDouble(GetDouble());
                break;
            case LanguageSymbols.DefTypesName.Index.Single:
                converter.CastSingle(GetSingle());
                break;
            case LanguageSymbols.DefTypesName.Index.String:
                converter.CastString(GetString());
                break;
            case LanguageSymbols.DefTypesName.Index.Bool:
                converter.CastBoolean(GetBoolean());
                break;
        }
    }

    private void CheckValue(string cSharpType)
    {
        Compilation.Assert(!string.IsNullOrEmpty(m_typeName), "Can't cast unknown type to value of type" + cSharpType, -1);
        Compilation.Assert(!string.IsNullOrEmpty(StringVal), "Can't cast empty string to value of type" + cSharpType, -1);

        var lang = LanguageSymbols.Instance;
        if(lang.GetDefaultTypeId(lang.GetTypeNameByCSharpTypeName(cSharpType)) != m_typeId)
        {
            Compilation.WriteError(string.Format("Can't cast from type {0} to CSharp type {1}", m_typeName, cSharpType), -1);
        }
    }

    private string m_typeName;
    private sbyte m_typeId;
}