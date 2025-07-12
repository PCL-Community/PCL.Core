using System;

namespace PCL.Core.Model.Mod.Modrinth;

[Serializable]
public record ModrinthDonationUrl(
    string id,
    string platform,
    string url);