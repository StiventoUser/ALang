using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// Represents a file containing list of lexemes and additional information
/// </summary>
public sealed class LexemeModule
{
    public List<Lexeme> Lexemes;
    public string FileName;
}
/// <summary>
/// Stores part of input source code
/// </summary>
public sealed class Lexeme
{
    public string source;

    public enum CodeType { Reserved, Number, Name, Delimiter, String };
    public CodeType codeType;

    public int line;
};
public sealed class Lexer
{
    public Lexer()
    {
        m_delimiters = m_delimiters.OrderByDescending(delim => delim.Length).ToList();
    }

    public void Convert(List<SourceFileInfo> sources)
    {
        int pos;

        foreach(var source in sources)
        {
            pos = 0;
            m_lexemes = new List<Lexeme>();

            while(pos < source.SourceCode.Length)
            {
                pos = FindLexerPart(source.SourceCode, pos);
            }

            m_output.Add(new LexemeModule{ FileName = source.FileName, Lexemes = m_lexemes });
        }
    }
    public List<LexemeModule> GetOutput()
    {
        return m_output;
    }

    private int FindLexerPart(string source, int pos)
    {
        if(source[pos] == '/' && source[pos+1] == '/')
        {
            return RemoveComment(source, pos);
        }
        else if(Char.IsLetter(source[pos]))
        {
            return ReadWord(source, pos);
        }
        else if(m_delimiters.Exists(delim => delim[0] == source[pos]))
        {
            return ReadDelimiter(source, pos);   
        }
        else if(Char.IsDigit(source[pos]))
        {
            return ReadNumber(source, pos);
        }
        else if(source[pos] == '\"')
        {
            return ReadString(source, pos);
        }
        else if(Char.IsWhiteSpace(source[pos]))
        {
            if(source[pos] == '\n')
            {
                m_currentLine++;
            }
            return pos + 1;
        }
        else
        {
            throw new System.Exception("Unknown lexeme: " + source[pos]);
        }
    }

    private int RemoveComment(string source, int pos)
    {
        pos += 2;//skip '//'

        while(source[pos] != '\n')
            ++pos;
        return ++pos;
    }
    private int ReadWord(string source, int pos)
    {
        StringBuilder builder = new StringBuilder();

        while(Char.IsLetterOrDigit(source[pos]) || source[pos] == '_')
        {
            builder.Append(source[pos]);
            
            ++pos;            
        }

        string result = builder.ToString();
        m_lexemes.Add(new Lexeme{ source = result, codeType = GetIsReserved(result), line = m_currentLine });

        return pos;
    }
    private int ReadDelimiter(string source, int pos)
    {
        int index = m_delimiters.FindIndex(delim => (pos + delim.Length ) > source.Length ? false : delim == source.Substring(pos, delim.Length));

        if(index == -1)
        {
            //WTF?
            throw new System.Exception("WTF with delimiters");
        }

        m_lexemes.Add(new Lexeme{ source = m_delimiters[index], codeType = Lexeme.CodeType.Delimiter, line = m_currentLine });
        pos += m_delimiters[index].Length;

        return pos;
    }
    private int ReadNumber(string source, int pos)
    {
        StringBuilder builder = new StringBuilder();

        int pointCount = 0;
        while(Char.IsDigit(source[pos]) || (source[pos] == '.' && pointCount++ == 0))
        {
            builder.Append(source[pos]);
            
            ++pos;            
        }

        string result = builder.ToString();
        m_lexemes.Add(new Lexeme{ source = result, codeType = Lexeme.CodeType.Number, line = m_currentLine });

        return pos;
    }
    private int ReadString(string source, int pos)
    {
        StringBuilder builder = new StringBuilder();

        ++pos;//Skip "
        while(source[pos] != '\"')
        {
            if(source[pos] == '\\')
                continue;

            builder.Append(source[pos]);
            
            ++pos;            
        }
        ++pos;//Skip "

        string result = builder.ToString();
        m_lexemes.Add(new Lexeme{ source = result, codeType = Lexeme.CodeType.String, line = m_currentLine });

        return pos;
    }

    Lexeme.CodeType GetIsReserved(string source)
    {
        if(m_reserved.Contains(source))
            return Lexeme.CodeType.Reserved;
        else
            return Lexeme.CodeType.Name;
    }

    List<LexemeModule> m_output = new List<LexemeModule>();
    List<Lexeme> m_lexemes;
    int m_currentLine = 0;
    List<string> m_reserved = new List<string>{ "if", "else", "for", "while", "function", "class", "constexpr",
                                                "using", "component", "default",
                                                "Int8", "Int16", "Int32", "Int64", "Bool",
                                                "Single", "Double", "String" };
    List<string> m_delimiters = new List<string>{ "+", "-", "*", "/", ",", ".", "<", ">", "<=",
                                                  ">=", "=", "!=", ";", ":", "%", "^",
                                                  "(", ")", "[", "]", "{", "}", "->" };
}