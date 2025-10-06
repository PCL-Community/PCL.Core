
using PCL.Core.Net;

namespace PCL.Core.Minecraft.Accounts.Models;

public class OAuthProfile
{
    public required string TokenEndpoint;
    public required string UserInfoEndpoint;
    /// <summary>
    /// 当前账户的 IdToken (OAuth)
    /// </summary>
    public string? IdToken;
    public override bool ValidateAsync()
    {
           using var response = HttpRequestBuilder.Create()
    }
}