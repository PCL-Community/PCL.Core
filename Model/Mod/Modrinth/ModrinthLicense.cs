using System;

namespace PCL.Core.Model.Mod.Modrinth;

[Serializable]
public record ModrinthLicense(
    string id,
    string name,
    string? url);