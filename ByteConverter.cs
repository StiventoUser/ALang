using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ALang
{
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
            get { return m_bytes; }
        }

        /// <summary>
        /// Creates new empty converter
        /// </summary>
        /// <returns></returns>
        public static ByteConverter New()
        {
            return new ByteConverter {m_bytes = new List<byte>()};
        }

        /// <summary>
        /// Creates new converter initialized passed bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static ByteConverter New(IList<byte> bytes)
        {
            return new ByteConverter {m_bytes = bytes.ToList()};
        }

        /// <summary>
        /// Adds value as set of bits
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bitCount"></param>
        /// <returns></returns>
        public ByteConverter CastToBits(int val, int bitCount)
        {
            var bytes = BitConverter.GetBytes(val);
            int bytesCount = bitCount / 8 + ((bitCount % 8) == 0 ? 0 : 1);
            byte bitsCount = (byte) (bitCount - 8 * bytesCount);

            bytes[bytesCount - 1] = (byte) (bytes[bytesCount - 1] & (0xff >> (8 - bitsCount)));
            m_bytes.AddRange(bytes);

            return this;
        }

        /// <summary>
        /// Add byte
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public ByteConverter CastByte(sbyte val)
        {
            unchecked
            {
                m_bytes.Add((byte) val);
            }
            return this;
        }

        /// <summary>
        /// Int16 to bytes
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public ByteConverter CastInt16(Int16 val)
        {
            m_bytes.AddRange(BitConverter.GetBytes(val));

            return this;
        }

        /// <summary>
        /// Int32 to bytes
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public ByteConverter CastInt32(Int32 val)
        {
            m_bytes.AddRange(BitConverter.GetBytes(val));

            return this;
        }

        /// <summary>
        /// Int64 to bytes
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public ByteConverter CastInt64(Int64 val)
        {
            m_bytes.AddRange(BitConverter.GetBytes(val));

            return this;
        }

        /// <summary>
        /// Double to bytes
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public ByteConverter CastDouble(Double val)
        {
            m_bytes.AddRange(BitConverter.GetBytes(val));

            return this;
        }

        /// <summary>
        /// Single to bytes
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public ByteConverter CastSingle(Single val)
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

            CastInt32(bytes.Length);
            m_bytes.AddRange(bytes);

            return this;
        }

        /// <summary>
        /// Boolean to bytes
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public ByteConverter CastBoolean(Boolean val)
        {
            m_bytes.AddRange(BitConverter.GetBytes(val));

            return this;
        }

        /// <summary>
        /// StringToValue to bytes
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public ByteConverter CastValueContainer(StringToValue val)
        {
            val.WriteBytes(this);

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
        public Int32 GetInt32()
        {
            if ((m_pos + 4) > m_bytes.Count)
            {
                Compilation.WriteError("Broken operation argument. No free bytes", -1);
            }

            int val = m_bytes[m_pos] | (m_bytes[m_pos + 1] << 8) | (m_bytes[m_pos + 2] << 16) | (m_bytes[m_pos + 3] << 24);

            m_pos += sizeof(Int32);

            return val;
        }

        /// <summary>
        /// Bytes to long
        /// </summary>
        /// <returns></returns>
        public Int64 GetInt64()
        {
            if ((m_pos + 8) > m_bytes.Count)
            {
                Compilation.WriteError("Broken operation argument. No free bytes", -1);
            }

            long val = (Int64)m_bytes[m_pos] | (Int64)(m_bytes[m_pos + 1] << 8) | (Int64)(m_bytes[m_pos + 2] << 16) | (Int64)(m_bytes[m_pos + 3] << 24) |
                       (Int64)(m_bytes[m_pos] << 32) | (Int64)(m_bytes[m_pos + 1] << 40) | (Int64)(m_bytes[m_pos + 2] << 48) | (Int64)(m_bytes[m_pos + 3] << 56);

            m_pos += sizeof(Int64);

            return val;
        }

        private List<byte> m_bytes;
        private int m_pos = 0;
    }
}