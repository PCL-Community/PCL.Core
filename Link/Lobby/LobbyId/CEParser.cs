using System;
using PCL.Core.Utils.Exts;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Lobby.LobbyId;

public class CEParser : IParser
{
    public bool Validate(string code)
    {
        if (code.Length != 10) return false;
        try
        {
            _ = code.FromB32ToB10();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public LobbyInfo Parse(string code)
    {
        var info = code.FromB32ToB10();
        return new LobbyInfo
        {
            OriginalCode = code,
            NetworkName = info[..8],
            NetworkSecret = info[8..10],
            Port = int.Parse(info[10..]),
            Type = LobbyType.PCLCE,
            Ip = "10.114.51.41"
        };
    }
}