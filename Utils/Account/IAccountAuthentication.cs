using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Account;

public interface IAccountAuthentication<TAuthenticationData>
{
    Task<TAuthenticationData> AuthenticateAsync(CancellationToken? cancellationToken = null);

    event EventHandler OnAuthenticationStatusChanged;
}