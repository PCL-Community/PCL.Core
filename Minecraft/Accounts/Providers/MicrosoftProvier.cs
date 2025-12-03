using System.Threading.Tasks;

namespace PCL.Core.Minecraft.Accounts.Providers;

public class MicrosoftProvider:IAuthenticationProvider
{
    public Task<bool> ValidateAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task AuthenticateAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task RefreshAsync()
    {
        throw new System.NotImplementedException();
    }
}