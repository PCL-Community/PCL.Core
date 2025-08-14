using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Models;

public class ModrinthHashes
{
    [JsonPropertyName("sha512")] public required string Sha512;
    [JsonPropertyName("sha1")] public required string Sha1;
}