using System.Threading.Tasks;

namespace PCL.Core.Utils.Account;

public interface IMinecraftcAccount
{
    string Username { get; }
    string Uuid { get; }

    Task<string> GetSkin();
    Task<string> GetSkinHead();
}