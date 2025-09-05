using System.Collections.Generic;
using PCL.Core.Minecraft.Instance;

namespace PCL.Core.Minecraft.Launch.State;

public record LaunchOptions(
    string? ServerIp,
    string? WorldName,
    string? SaveBatch,
    McInstance? Version,
    List<string> ExtraArgs,
    bool Test
);