using System.Collections.Generic;
using PCL.Core.Minecraft.Instance;
using PCL.Core.Minecraft.Instance.Interface;

namespace PCL.Core.Minecraft.Launch.State;

public record LaunchOptions (
    JavaInfo? Java,
    string? ServerIp,
    string? WorldName,
    string? SaveBatch,
    IMcInstance? Version,
    List<string> ExtraArgs,
    bool Test
);