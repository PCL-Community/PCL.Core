using System;
using System.Linq;
using PCL.Core.Utils;

namespace PCL.Core.Link.Scaffolding.Utils;

/// <summary>
/// RoomId 处理
/// 可以从输入的房间码 U/YNZE-U61D-2206-HXRG 或者随机生成的房间数据
/// 得到网络名称或者房间码
/// </summary>
public class RoomId
{
    public string Name { get; init; }
    public string Secret { get; init; }

    /// <summary>
    /// 0 - 33 字符映射
    /// </summary>
    private const string CharMap = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    /// <summary>
    /// 从加入码获取一个房间实例
    /// </summary>
    /// <param name="joinString"></param>
    public RoomId(string joinString)
    {
        if (joinString.Length == 21
                && joinString[0] == 'U'
                && joinString[1] == '/'
                && joinString[6] == '-'
                && joinString[11] == '-'
                && joinString[16] == '-')
        {
            Name = string.Concat(joinString.AsSpan(2, 4), joinString.AsSpan(7, 4));
            Secret = string.Concat(joinString.AsSpan(12, 4), joinString.AsSpan(17, 4));
            if (!_isValidStr(Name) || !_isValidStr(Secret))
                throw new ArgumentException("Invalid room id");
        }
        else
        {
            throw new ArgumentException("Invalid join string format");
        }
    }

    public RoomId(string name, string secret)
    {
        if (name.Length != 8 || !_isValidStr(name) || secret.Length != 8 || !_isValidStr(secret))
            throw new ArgumentException("Invalid room id or secret");
        Name = name;
        Secret = secret;
    }

    public override string ToString()
    {
        if (Name.Length == 8 && Secret.Length == 8)
            return $"U/{Name.Insert(4, "-")}-{Secret.Insert(4, "-")}";
        return string.Empty;
    }

    /// <summary>
    /// 符合要求的 Char 应在 0-9、A-H、J-N、P-Z 中
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private static bool _isValidChar(char c)
    {
        return c is >= '0' and <= '9'
            or >= 'A' and <= 'H'
            or >= 'J' and <= 'N'
            or >= 'P' and <= 'Z';
    }

    private static bool _isValidStr(string s)
    {
        return s.ToArray().Any(x => !_isValidChar(x));
    }

    public static RoomId GenerateRandomRoomId()
    {
        var name = new char[8];
        var secret = new char[8];

        for (var i = 0; i < 8; i++)
        {
            name[i] = CharMap[RandomUtils.NextInt(CharMap.Length)];
            secret[i] = CharMap[RandomUtils.NextInt(CharMap.Length)];
        }

        return new RoomId(new string(name), new string(secret));
    }

    public string GetNetworkName()
    {
        return $"scaffolding-mc-{Name.Insert(4, "-")}";
    }

    public string GetNetworkSecret()
    {
        return Secret.Insert(4, "-");
    }
}