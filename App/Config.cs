using System;
using PCL.Core.App.Configuration;

namespace PCL.Core.App;

public static partial class Config
{
    [ConfigItem<string>("Identify", "")] public static partial string Identifier { get; set; }

    [ConfigGroup("Count")] partial class CountConfigGroup
    {
        [ConfigItem<bool>("Eula", false)] public partial bool Eula { get; set; }
        [ConfigItem<bool>("HintDownloadThread", false)] public partial bool DownloadThreadWarning { get; set; }
        [ConfigItem<int>("SystemCount", 0)] public partial int Startup { get; set; }
    }

    [ConfigGroup("System")] partial class SystemConfigGroup
    {
        [AnyConfigItem<DateTime>("LastUpdate")] public partial DateTime LastUpdate { get; set; }

        [ConfigGroup("Ano")] partial class AnotherGroup
        {
            [ConfigItem<int>("Ano", 123)] public partial ArgConfig<int> Ano { get; }
        }
    }

    [ConfigGroup("Instance")] public partial class InstanceConfigGroup
    {
        [ConfigItem<int>("SystemLaunchCount", 12, ConfigSource.GameInstance)] public partial int LaunchGame { get; set; }
    }

    [RegisterConfigEvent]
    public static ConfigEventRegistry SystemChanged => new(
        scope: System,
        trigger: ConfigEvent.All,
        handler: _ =>
        {
        }
    );
}
