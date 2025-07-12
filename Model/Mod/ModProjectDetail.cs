using System.Collections.Generic;

namespace PCL.Core.Model.Mod;

public record ModProjectDetail(
    string Title,
    string Slug,
    string Id,
    string Author,
    string Description,
    List<string> Categories,
    List<string> GameVersions,
    bool SupportClient,
    bool SupportServer,
    long Downloads,
    string Icon);