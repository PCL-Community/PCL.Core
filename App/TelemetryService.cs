using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Net.Nat.Stun;
using PCL.Core.ProgramSetup;
using PCL.Core.Utils.OS;

namespace PCL.Core.App;

[LifecycleService(LifecycleState.Running)]
public class TelemetryService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private TelemetryService() : base("Telemetry", "遥测数据服务") { _context = Lifecycle.GetContext(this); }

    private class TelemetryData
    {
        public required string Tag { get; set; }
        public required string Id { get; set; }
        [JsonPropertyName("OS")]
        public required int Os { get; set; }
        public required bool Is64Bit { get; set; }
        [JsonPropertyName("IsARM64")]
        public required bool IsArm64 { get; set; }
        public required string Launcher { get; set; }
        public required string LauncherBranch {get; set; }
        [JsonPropertyName("UsedOfficialPCL")]
        public required bool UsedOfficialPcl { get; set; }
        [JsonPropertyName("UsedHMCL")]
        public required bool UsedHmcl { get; set; }
        [JsonPropertyName("UsedBakaXL")]
        public required bool UsedBakaXl { get; set; }
        public required ulong Memory {get; set; }
        public required string NatMapBehaviour {get; set; }
        public required string NatFilterBehaviour {get; set; }
        [JsonPropertyName("IPv6Status")]
        public required string Ipv6Status {get; set; }
    }

    public override void Start()
    {
        if (!Setup.System.Telemetry) return;
        var telemetryKey = EnvironmentInterop.GetSecret("TELEMETRY_KEY");
        if (string.IsNullOrWhiteSpace(telemetryKey)) return;
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var natTest = new NatTest(new StunClient());
        // var natResult = natTest.DetectNatTypeAsync().Result;
        var hasPublicIpv6Address = false;
        var telemetry = new TelemetryData
        {
            Tag = "Telemetry",
            Id = Utils.Secret.Identify.LaunchId,
            Os = Environment.OSVersion.Version.Build,
            Is64Bit = Environment.Is64BitOperatingSystem,
            IsArm64 = RuntimeInformation.OSArchitecture.Equals(Architecture.Arm64),
            Launcher = "",
            LauncherBranch = Setup.System.UpdateBranch switch
            {
                0 => "Slow Ring",
                1 => "Fast Ring",
                _ => "Unknown"
            },
            UsedOfficialPcl =
                bool.TryParse(Registry.GetValue(@"HKEY_CURRENT_USER\Software\PCL", "SystemEula", "false") as string,
                    out var officialPcl) && officialPcl,
            UsedHmcl = Directory.Exists(Path.Combine(appDataFolder, ".hmcl")),
            UsedBakaXl = Directory.Exists(Path.Combine(appDataFolder, "BakaXL")),
            Memory = KernelInterop.GetPhysicalMemoryBytes().Total,
            NatMapBehaviour = "", //natResult.MappingBehavior.ToString(),
            NatFilterBehaviour = "", //natResult.FilteringBehavior.ToString(),
            Ipv6Status = "Unknown"
        };
        var sendData = JsonSerializer.Serialize(telemetry);
        using var response = HttpRequestBuilder
            .Create("https://pcl2ce.pysio.online/post", HttpMethod.Post)
            .WithAuthentication(telemetryKey)
            .SendAsync().Result;
        if (response.IsSuccess)
            LogWrapper.Info("Telemetry", "已发送调查数据");
        else
            LogWrapper.Error("Telemetry", "调查数据发送失败，请检查网络连接以及使用的版本");
    }
}