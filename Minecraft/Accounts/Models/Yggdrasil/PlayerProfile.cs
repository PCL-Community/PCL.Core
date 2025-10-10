using PCL.Core.Logging;
using PCL.Core.Utils.Exts;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCL.Core.Utils.Accounts.Models.Yggdrasil;
public record PlayerProfile 
{
    [JsonPropertyName("name")] public required string Name;
    [JsonPropertyName("id")] public required string Uuid;
    [JsonPropertyName("properties")] public required List<ProfileProperty> Properties 
    {
        get => throw new InvalidOperationException("It a write-only property.");
        set
        {
            foreach(var property in value)
            {
                switch (property.Name)
                {
                    case "textures":
                        var texturesString = property.Value.FromB64ToStr();
                        var texture = JsonSerializer.Deserialize<Textures>(texturesString) ?? 
                            throw new FormatException("Failed to parse texture info");
                        Skin = texture.Skin;
                        Cape = texture.Cape;
                        break;
                    case "uploadableTextures":
                        foreach(var uploadable in property.Value.Split(","))
                        {
                            if (uploadable == "skin") UploadSkin = true;
                            if (uploadable == "cape") UploadCape = true;
                        }
                        break;
                    default:
                        LogWrapper.Warn("Profile", $"Unknown property {property.Name}");
                        break;
                }
            }
        }
    }
    internal bool UploadSkin;
    internal bool UploadCape;
    internal Texture? Skin;
    internal Texture? Cape;
}

public record ProfileProperty
{
    [JsonPropertyName("name")] public required string Name;
    [JsonPropertyName("value")] public required string Value;
    [JsonPropertyName("signature")] public string? Signature;
}

public record TextureData
{
    [JsonPropertyName("timestamp")] public long Timestamp;
    [JsonPropertyName("profileId")] public required string ProfileId;
    [JsonPropertyName("profileName")] public required string ProfileName;
    [JsonPropertyName("textures")] public required Textures Textures;
}

public record Textures
{
    [JsonPropertyName("skin")] public Texture? Skin;
    [JsonPropertyName("cape")] public Texture? Cape;
}

public record Texture
{
    [JsonPropertyName("url")] public required string Url;
    [JsonPropertyName("metadata")] public required TextureMetaData MetaData;
}

public record TextureMetaData
{
    [JsonPropertyName("model")] public required string Model;
}