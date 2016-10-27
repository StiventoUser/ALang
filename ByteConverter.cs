using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public sealed class ByteConverter
{
    public IList<byte> Bytes
    {
        get{ return m_bytes; }
    }
    
    public static ByteConverter New()
    {
        return new ByteConverter{ m_bytes = new List<byte>() };
    }
    public static ByteConverter New(IList<byte> bytes)
    {
        return new ByteConverter{ m_bytes = bytes.ToList() };
    }

    public ByteConverter Cast(int val)
    {
        m_bytes.AddRange(BitConverter.GetBytes(val));

        return this;
    }
    public ByteConverter Cast(string val)
    {
        var bytes = Encoding.Unicode.GetBytes(val);
        
        this.Cast(bytes.Length);
        m_bytes.AddRange(bytes);

        return this;
    }

    public ByteConverter SkipBytes(int count)
    {
        m_pos += count;
        return this;
    }
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