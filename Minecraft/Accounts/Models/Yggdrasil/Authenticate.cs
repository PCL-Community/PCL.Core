using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Accounts.Models.Yggdrasil
{
    public record Authenticate
    {
        [JsonPropertyName("username")] public required string UserName;
        [JsonPropertyName("password")] public required string Password;
        [JsonPropertyName("requestUser")] public bool RequestUser = false;
        [JsonPropertyName("agent")] public Agent Agent = new();
    }
    public record Agent
    {
        [JsonPropertyName("name")] public string Name = "minecraft";
        [JsonPropertyName("version")] public int Version = 1;
    }

    public record AuthenticateResponse
    {
        [JsonPropertyName("errorMessage")] public string? ErrorMessage;
        [JsonPropertyName("accessToken")] public string? AccessToken;
        [JsonPropertyName("clientToken")] public string? ClientToken;
        [JsonPropertyName("availableProfiles")] public required List<PlayerProfile?> AvailableProfile;
        [JsonPropertyName("selectedProfile")] public PlayerProfile? SelectedProfile;
    }

}
