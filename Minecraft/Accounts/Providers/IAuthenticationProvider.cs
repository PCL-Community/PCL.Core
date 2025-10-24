using System.Threading.Tasks;

namespace PCL.Core.Minecraft.Accounts.Providers;

public interface IAuthenticationProvider
{
    Task<bool> ValidateAsync();
    Task AuthenticateAsync();
    Task RefreshAsync();
    
    
}