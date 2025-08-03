using System;

namespace PCL.Core.Utils.Account;

public interface IAuthenticationService
{
    bool IsAuthenticated { get; }
    string UserName { get; }
    event EventHandler OnAuthenticationStatusChanged;
}