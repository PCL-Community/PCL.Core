using System;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft.Accounts.Providers;

public class YggdrasilAuthenticateProvider:IAuthenticationProvider
{
    private string _username;
    private string _password;

    public YggdrasilAuthenticateProvider(string username, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(password);
        _username = username;
        _password = password;
    }
    
    public async Task<bool> ValidateAsync()
    {
        throw new NotImplementedException();
    }

    public async Task AuthenticateAsync()
    {
        throw new NotImplementedException();
    }

    public async Task RefreshAsync()
    {
        throw new NotImplementedException();
    }
}