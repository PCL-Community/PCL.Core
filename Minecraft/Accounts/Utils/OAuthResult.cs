using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Accounts.Utils;

public record OAuthResult
{
    [JsonPropertyName("error")]
    public string Error
    {
        get => _error;
        set
        {
            _error = value;
            Succeed = false;
        }
    }
    
    private string? _error;

    [JsonPropertyName("error_description")]
    public string? Description;

    public bool Succeed;

    [JsonPropertyName("access_token")]public string? AccessToken;

    [JsonPropertyName("refresh_token")]public string? RefreshToken;

    [JsonPropertyName("id_token")]public string? IdToken;

}