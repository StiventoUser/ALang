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
    /// Adds value as set of bits
    /// </summary>
    /// <param name="val"></param>
    /// <param name="bitSize"></param>
    /// <returns></returns>
    public ByteConverter CastToBits(int val, int bitCount)
    {
        var bytes = BitConverter.GetBytes(val);
        int bytesCount = bitCount / 8 + ((bitCount % 8) == 0 ? 0 : 1);
        byte bitsCount = (byte)(bitCount - 8 * bytesCount);

        bytes[bytesCount-1] = (byte)(bytes[bytesCount-1] & (0xff >> (8 - bitsCount)));
        m_bytes.AddRange(bytes);

        return this;
    }
    /// <summary>
    /// Int to bytes
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public ByteConverter CastInt32(int val)
    {
        m_bytes.AddRange(BitConverter.GetBytes(val));

        return this;
    }

    /// <summary>
    /// String to bytes
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public ByteConverter CastString(string val)
    {
        var bytes = Encoding.Unicode.GetBytes(val);
        
        this.CastInt32(bytes.Length);
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