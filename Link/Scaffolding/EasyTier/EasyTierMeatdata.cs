using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using PCL.Core.IO;

namespace PCL.Core.Link.Scaffolding.EasyTier;

public static class EasyTierMeatdata
{
    public const string CurrentEasyTierVer = "2.4.5";

    public static string EasyTierFilePath => Path.Combine(FileService.LocalDataPath, "EasyTier",
        CurrentEasyTierVer,
        $"easytier-windows-{(RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x86_64")}");
}