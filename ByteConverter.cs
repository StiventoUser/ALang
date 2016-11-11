using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// Converts types to byte array or vice versa
/// </summary>
public sealed class ByteConverter
{
    /// <summary>
    /// Returns result of convertions
    /// </summary>
    /// <returns></returns>
    public IList<byte> Bytes
    {
        get{ return m_bytes; }
    }
    
    /// <summary>
    /// Creates new empty converter
    /// </summary>
    /// <returns></returns>
    public static ByteConverter New()
    {
        return new ByteConverter{ m_bytes = new List<byte>() };
    }

    /// <summary>
    /// Creates new converter initialized passed bytes
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static ByteConverter New(IList<byte> bytes)
    {
        return new ByteConverter{ m_bytes = bytes.ToList() };
    }

    /// <summary>
    /// Int to bytes
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public ByteConverter Cast(int val)
    {
        m_bytes.AddRange(BitConverter.GetBytes(val));

        return this;
    }

    /// <summary>
    /// String to bytes
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public ByteConverter Cast(string val)
    {
        var bytes = Encoding.Unicode.GetBytes(val);
        
        this.Cast(bytes.Length);
        m_bytes.AddRange(bytes);

        return this;
    }

    /// <summary>
    /// Ignore some bytes to use next
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public ByteConverter SkipBytes(int count)
    {
        m_pos += count;
        return this;
    }

    /// <summary>
    /// Bytes to int
    /// </summary>
    /// <returns></returns>
    public int GetInt32()
    {
        if((m_pos + 4) > m_bytes.Count)
        {
            Compilation.WriteError("Broken operation argument. No free bytes", -1);
        }

        int val = m_bytes[m_pos] | (m_bytes[m_pos+1] << 8) | (m_bytes[m_pos+2] << 16) | (m_bytes[m_pos+3] << 24);

        return val;
    }

    private List<byte> m_bytes;
    private int m_pos = 0;
}