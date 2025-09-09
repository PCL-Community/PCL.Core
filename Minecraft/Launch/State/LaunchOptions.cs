using System.Collections.Generic;
using PCL.Core.Minecraft.Instance;

namespace PCL.Core.Minecraft.Launch.State;

public record LaunchOptions (
    JavaInfo? Java,
    string? ServerIp,
    string? WorldName,
    string? SaveBatch,
    McNoPatchesInstance? Version,
    List<string> ExtraArgs,
    bool Test
);