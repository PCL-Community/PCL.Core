using System.Collections.Generic;

namespace PCL.Core.Minecraft.McLaunch.State;

public record LaunchOptions(
    string? ServerIp,
    string? WorldName,
    string? SaveBatch,
    McInstance.McInstance? Version,
    List<string> ExtraArgs,
    bool Test
);