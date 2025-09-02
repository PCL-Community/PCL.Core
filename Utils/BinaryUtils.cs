using System;
using System.Text;

namespace PCL.Core.Utils;

public static class BinaryUtils
{
    /// <summary>
    /// 判断当前系统是否是小端序
    /// </summary>
    public static bool IsLittleEndian => BitConverter.IsLittleEndian;

    #region 字节数组反转

    /// <summary>
    /// 反转字节数组（大端序与小端序互换）
    /// </summary>
    public static byte[] ReverseBytes(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        byte[] reversed = new byte[bytes.Length];
        Array.Copy(bytes, reversed, bytes.Length);
        Array.Reverse(reversed);
        return reversed;
    }

    /// <summary>
    /// 反转字节数组的指定部分
    /// </summary>
    public static void ReverseBytes(byte[] bytes, int startIndex, int length)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex >= bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        if (length < 0 || startIndex + length > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(length));

        Array.Reverse(bytes, startIndex, length);
    }

    #endregion

    #region Int16 转换

    /// <summary>
    /// 将小端序字节数组转换为Int16（主机字节序）
    /// </summary>
    public static short ToInt16FromLittleEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 2 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (IsLittleEndian)
            return BitConverter.ToInt16(bytes, startIndex);

        byte[] reversed = new byte[2];
        Array.Copy(bytes, startIndex, reversed, 0, 2);
        Array.Reverse(reversed);
        return BitConverter.ToInt16(reversed, 0);
    }

    /// <summary>
    /// 将大端序字节数组转换为Int16（主机字节序）
    /// </summary>
    public static short ToInt16FromBigEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 2 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (!IsLittleEndian)
            return BitConverter.ToInt16(bytes, startIndex);

        byte[] reversed = new byte[2];
        Array.Copy(bytes, startIndex, reversed, 0, 2);
        Array.Reverse(reversed);
        return BitConverter.ToInt16(reversed, 0);
    }

    /// <summary>
    /// 将Int16转换为小端序字节数组
    /// </summary>
    public static byte[] GetLittleEndianBytes(short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>
    /// 将Int16转换为大端序字节数组
    /// </summary>
    public static byte[] GetBigEndianBytes(short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    #endregion

    #region UInt16 转换

    /// <summary>
    /// 将小端序字节数组转换为UInt16（主机字节序）
    /// </summary>
    public static ushort ToUInt16FromLittleEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 2 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (IsLittleEndian)
            return BitConverter.ToUInt16(bytes, startIndex);

        byte[] reversed = new byte[2];
        Array.Copy(bytes, startIndex, reversed, 0, 2);
        Array.Reverse(reversed);
        return BitConverter.ToUInt16(reversed, 0);
    }

    /// <summary>
    /// 将大端序字节数组转换为UInt16（主机字节序）
    /// </summary>
    public static ushort ToUInt16FromBigEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 2 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (!IsLittleEndian)
            return BitConverter.ToUInt16(bytes, startIndex);

        byte[] reversed = new byte[2];
        Array.Copy(bytes, startIndex, reversed, 0, 2);
        Array.Reverse(reversed);
        return BitConverter.ToUInt16(reversed, 0);
    }

    /// <summary>
    /// 将UInt16转换为小端序字节数组
    /// </summary>
    public static byte[] GetLittleEndianBytes(ushort value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>
    /// 将UInt16转换为大端序字节数组
    /// </summary>
    public static byte[] GetBigEndianBytes(ushort value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    #endregion

    #region Int32 转换

    /// <summary>
    /// 将小端序字节数组转换为Int32（主机字节序）
    /// </summary>
    public static int ToInt32FromLittleEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 4 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (IsLittleEndian)
            return BitConverter.ToInt32(bytes, startIndex);

        byte[] reversed = new byte[4];
        Array.Copy(bytes, startIndex, reversed, 0, 4);
        Array.Reverse(reversed);
        return BitConverter.ToInt32(reversed, 0);
    }

    /// <summary>
    /// 将大端序字节数组转换为Int32（主机字节序）
    /// </summary>
    public static int ToInt32FromBigEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 4 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (!IsLittleEndian)
            return BitConverter.ToInt32(bytes, startIndex);

        byte[] reversed = new byte[4];
        Array.Copy(bytes, startIndex, reversed, 0, 4);
        Array.Reverse(reversed);
        return BitConverter.ToInt32(reversed, 0);
    }

    /// <summary>
    /// 将Int32转换为小端序字节数组
    /// </summary>
    public static byte[] GetLittleEndianBytes(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>
    /// 将Int32转换为大端序字节数组
    /// </summary>
    public static byte[] GetBigEndianBytes(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    #endregion

    #region UInt32 转换

    /// <summary>
    /// 将小端序字节数组转换为UInt32（主机字节序）
    /// </summary>
    public static uint ToUInt32FromLittleEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 4 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (IsLittleEndian)
            return BitConverter.ToUInt32(bytes, startIndex);

        byte[] reversed = new byte[4];
        Array.Copy(bytes, startIndex, reversed, 0, 4);
        Array.Reverse(reversed);
        return BitConverter.ToUInt32(reversed, 0);
    }

    /// <summary>
    /// 将大端序字节数组转换为UInt32（主机字节序）
    /// </summary>
    public static uint ToUInt32FromBigEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 4 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (!IsLittleEndian)
            return BitConverter.ToUInt32(bytes, startIndex);

        byte[] reversed = new byte[4];
        Array.Copy(bytes, startIndex, reversed, 0, 4);
        Array.Reverse(reversed);
        return BitConverter.ToUInt32(reversed, 0);
    }

    /// <summary>
    /// 将UInt32转换为小端序字节数组
    /// </summary>
    public static byte[] GetLittleEndianBytes(uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>
    /// 将UInt32转换为大端序字节数组
    /// </summary>
    public static byte[] GetBigEndianBytes(uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    #endregion

    #region Int64 转换

    /// <summary>
    /// 将小端序字节数组转换为Int64（主机字节序）
    /// </summary>
    public static long ToInt64FromLittleEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 8 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (IsLittleEndian)
            return BitConverter.ToInt64(bytes, startIndex);

        byte[] reversed = new byte[8];
        Array.Copy(bytes, startIndex, reversed, 0, 8);
        Array.Reverse(reversed);
        return BitConverter.ToInt64(reversed, 0);
    }

    /// <summary>
    /// 将大端序字节数组转换为Int64（主机字节序）
    /// </summary>
    public static long ToInt64FromBigEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 8 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (!IsLittleEndian)
            return BitConverter.ToInt64(bytes, startIndex);

        byte[] reversed = new byte[8];
        Array.Copy(bytes, startIndex, reversed, 0, 8);
        Array.Reverse(reversed);
        return BitConverter.ToInt64(reversed, 0);
    }

    /// <summary>
    /// 将Int64转换为小端序字节数组
    /// </summary>
    public static byte[] GetLittleEndianBytes(long value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>
    /// 将Int64转换为大端序字节数组
    /// </summary>
    public static byte[] GetBigEndianBytes(long value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    #endregion

    #region UInt64 转换

    /// <summary>
    /// 将小端序字节数组转换为UInt64（主机字节序）
    /// </summary>
    public static ulong ToUInt64FromLittleEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 8 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (IsLittleEndian)
            return BitConverter.ToUInt64(bytes, startIndex);

        byte[] reversed = new byte[8];
        Array.Copy(bytes, startIndex, reversed, 0, 8);
        Array.Reverse(reversed);
        return BitConverter.ToUInt64(reversed, 0);
    }

    /// <summary>
    /// 将大端序字节数组转换为UInt64（主机字节序）
    /// </summary>
    public static ulong ToUInt64FromBigEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 8 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (!IsLittleEndian)
            return BitConverter.ToUInt64(bytes, startIndex);

        byte[] reversed = new byte[8];
        Array.Copy(bytes, startIndex, reversed, 0, 8);
        Array.Reverse(reversed);
        return BitConverter.ToUInt64(reversed, 0);
    }

    /// <summary>
    /// 将UInt64转换为小端序字节数组
    /// </summary>
    public static byte[] GetLittleEndianBytes(ulong value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>
    /// 将UInt64转换为大端序字节数组
    /// </summary>
    public static byte[] GetBigEndianBytes(ulong value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    #endregion

    #region Single 转换

    /// <summary>
    /// 将小端序字节数组转换为Single（主机字节序）
    /// </summary>
    public static float ToSingleFromLittleEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 4 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (IsLittleEndian)
            return BitConverter.ToSingle(bytes, startIndex);

        byte[] reversed = new byte[4];
        Array.Copy(bytes, startIndex, reversed, 0, 4);
        Array.Reverse(reversed);
        return BitConverter.ToSingle(reversed, 0);
    }

    /// <summary>
    /// 将大端序字节数组转换为Single（主机字节序）
    /// </summary>
    public static float ToSingleFromBigEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 4 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (!IsLittleEndian)
            return BitConverter.ToSingle(bytes, startIndex);

        byte[] reversed = new byte[4];
        Array.Copy(bytes, startIndex, reversed, 0, 4);
        Array.Reverse(reversed);
        return BitConverter.ToSingle(reversed, 0);
    }

    /// <summary>
    /// 将Single转换为小端序字节数组
    /// </summary>
    public static byte[] GetLittleEndianBytes(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>
    /// 将Single转换为大端序字节数组
    /// </summary>
    public static byte[] GetBigEndianBytes(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    #endregion

    #region Double 转换

    /// <summary>
    /// 将小端序字节数组转换为Double（主机字节序）
    /// </summary>
    public static double ToDoubleFromLittleEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 8 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (IsLittleEndian)
            return BitConverter.ToDouble(bytes, startIndex);

        byte[] reversed = new byte[8];
        Array.Copy(bytes, startIndex, reversed, 0, 8);
        Array.Reverse(reversed);
        return BitConverter.ToDouble(reversed, 0);
    }

    /// <summary>
    /// 将大端序字节数组转换为Double（主机字节序）
    /// </summary>
    public static double ToDoubleFromBigEndian(byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (startIndex < 0 || startIndex + 8 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (!IsLittleEndian)
            return BitConverter.ToDouble(bytes, startIndex);

        byte[] reversed = new byte[8];
        Array.Copy(bytes, startIndex, reversed, 0, 8);
        Array.Reverse(reversed);
        return BitConverter.ToDouble(reversed, 0);
    }

    /// <summary>
    /// 将Double转换为小端序字节数组
    /// </summary>
    public static byte[] GetLittleEndianBytes(double value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    /// <summary>
    /// 将Double转换为大端序字节数组
    /// </summary>
    public static byte[] GetBigEndianBytes(double value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    #endregion

    #region 字符串编码转换

    /// <summary>
    /// 将UTF-8字符串转换为指定字节序的字节数组
    /// </summary>
    public static byte[] GetBytesWithEndian(string text, bool bigEndian = false)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);

        if (bigEndian && IsLittleEndian)
            Array.Reverse(bytes);
        else if (!bigEndian && !IsLittleEndian)
            Array.Reverse(bytes);

        return bytes;
    }

    /// <summary>
    /// 将指定字节序的字节数组转换为UTF-8字符串
    /// </summary>
    public static string GetStringWithEndian(byte[] bytes, bool bigEndian = false, int index = 0, int count = -1)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        if (count == -1)
            count = bytes.Length - index;

        byte[] stringBytes = new byte[count];
        Array.Copy(bytes, index, stringBytes, 0, count);

        if (bigEndian && IsLittleEndian)
            Array.Reverse(stringBytes);
        else if (!bigEndian && !IsLittleEndian)
            Array.Reverse(stringBytes);

        return Encoding.UTF8.GetString(stringBytes);
    }

    #endregion
}