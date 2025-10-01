using System;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Models;

[Serializable]
public record ModrinthDonationUrl(
    string Id,
    string Platform,
    string Url);