using System;

namespace PCL.Core.Model.ResourceProject.Modrinth;

[Serializable]
public record ModrinthLicense(
    string id,
    string name,
    string? url);