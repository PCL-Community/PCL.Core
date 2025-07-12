using System.Collections.Generic;
using PCL.Core.Utils.Minecraft;

namespace PCL.Core.Utils.ModPlatform;

public record ModSearch(
    string keyWord,
    ModProjectType ModProjectType,
    HashSet<ModLoaders>? modLoaders = null,
    HashSet<string>? categories = null,
    HashSet<string>? gameVersions = null,
    int ResultLimit = 30);