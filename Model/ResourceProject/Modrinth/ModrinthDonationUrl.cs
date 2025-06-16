using System;

namespace PCL.Core.Model.ResourceProject.Modrinth;

[Serializable]
public record ModrinthDonationUrl(
    string id,
    string platform,
    string url);