using System;

namespace PCL.Core.Utils.Account.MicrosoftAccount;

public class MicrosoftAccountData
{
    public required string AccessToken { get; set; }
    public required DateTime AccessTokenExpiredAt { get; set; }
    public required string Username { get; set; }
    public required string Uuid { get; set; }
    public required string SkinHeadId { get; set; }
    public required string Description { get; set; }
}