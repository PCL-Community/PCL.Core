using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PCL.Core.Net;
using PCL.Core.Logging;

namespace PCL.Core.Link;

public class ScaffoldingHandler
{
    public static byte[] GetResponse(BinaryReader reader)
    {
        int requestTypeLength = reader.ReadByte();
        string requestType = Encoding.ASCII.GetString(reader.ReadBytes(requestTypeLength)); // 请求类型始终以 ASCII 编码
        Int32 bodyLength = BitConverter.ToInt32(reader.ReadBytes(4), 0);
        var bodyBytes = reader.ReadBytes(bodyLength);
                        
        LogWrapper.Debug("Net", $"收到 TCP 数据");

        byte status = 0;
        
        // 处理请求
        string response;
        switch (requestType)
        {
            case "c:ping":
                response = Encoding.ASCII.GetString(bodyBytes);
                break;
            case "c:protocols":
                response = SupportedProtocols;
                break;
            case "c:server_port":
                response = "25565"; // TODO
                break;
            default:
                status = 255;
                response = "Not defined";
                break;
        }
        
        byte[] responseBodyBytes = Encoding.UTF8.GetBytes(response);
        uint responseBodyLength = (uint)responseBodyBytes.Length;
        
        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            writer.Write(status);
            _WriteUInt32BigEndian(writer, responseBodyLength);
            writer.Write(responseBodyBytes);

            return memoryStream.ToArray();
        }
    }

    /// <summary>
    /// 以大端序将 UInt32 写入 BinaryWriter
    /// </summary>
    private static void _WriteUInt32BigEndian(BinaryWriter writer, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        writer.Write(bytes);
    }
    
    /// <summary>
    /// 处理请求并输出为 Byte 数组
    /// </summary>
    /// <param name="type">请求类型</param>
    /// <param name="body">请求体</param>
    /// <param name="isJson">是否以 ASCII 编码</param>
    /// <returns></returns>
    private static byte[] _GetRequestBytes(string type, string body, bool isJson = false)
    {
        byte[] typeBytes;
        byte[] bodyBytes;
        if (isJson)
        {
            typeBytes = Encoding.UTF8.GetBytes(type);
            bodyBytes = Encoding.UTF8.GetBytes(body);
        }
        else
        {
            typeBytes = Encoding.ASCII.GetBytes(type);
            bodyBytes = Encoding.ASCII.GetBytes(body);
        }
        
        // 请求前置内容
        byte typeLength = isJson ? Encoding.ASCII.GetBytes(typeBytes.Length.ToString())[0] : Encoding.UTF8.GetBytes(typeBytes.Length.ToString())[0];
        UInt32 bodyLength = (UInt32)bodyBytes.Length;
        var requestBytes = new byte[1 + typeBytes.Length + 4 + bodyBytes.Length];
        
        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            writer.Write(typeLength);
            writer.Write(type);
            writer.Write(bodyLength);
            writer.Write(bodyBytes);

            return memoryStream.ToArray();
        }
    }

    private static string _SendAndGetResponse(byte[] data, bool isJson = false)
    {
        var client = new TcpClientHelper();
        var result = client.SendAndReceiveAsync(data).GetAwaiter().GetResult();

        return isJson ? Encoding.UTF8.GetString(result) : Encoding.ASCII.GetString(result);
    }

    public bool Ping()
    {
        var response = _SendAndGetResponse(_GetRequestBytes("c:ping", "Test"));
        return response == "Test";
    }

    public static string SupportedProtocols = "c:ping\0c:protocols\0c:server_port\0pclce:modpackId";
    
    public static string GetSupportedProtocols()
    {
        var response = _SendAndGetResponse(_GetRequestBytes("c:protocols", "Test"));
        return response;
    }

    public static int GetServerPort()
    {
        var response = _SendAndGetResponse(_GetRequestBytes("c:server_port", ""));
        return int.Parse(response);
    }
}