using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Account.MicrosoftAccount;

public class MicrosoftAuthentication : IAccountAuthentication<MicrosoftAuthenticationData>
{
    public Task<MicrosoftAuthenticationData> AuthenticateAsync(CancellationToken? cancellationToken = null)
    {
        throw new System.NotImplementedException();
    }

    public event EventHandler? OnAuthenticationStatusChanged;
}