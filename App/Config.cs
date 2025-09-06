using PCL.Core.App.Configuration;

namespace PCL.Core.App;

// ReSharper disable InconsistentNaming
public static partial class Config
{
    [ConfigGroup("Identify")] partial class IdentifyConfigGroup
    {
        [ConfigItem<string>("Identify", "")] public partial string Identifier { get; set; }
    }

    [ConfigGroup("Hint")] partial class HintConfigGroup
    {
        [ConfigItem<bool>("HintDownloadThread", false)] public partial bool DownloadThreadCount { get; set; }
        [ConfigItem<int>("HintNotice", 0)] public partial int Notice { get; set; }
        [ConfigItem<int>("HintDownload", 0)] public partial int Download { get; set; }
        [ConfigItem<bool>("HintInstallBack", false)] public partial bool InstallPageBack { get; set; }
        [ConfigItem<bool>("HintHide", false)] public partial bool HideGameInstance { get; set; }
        [ConfigItem<bool>("HintHandInstall", false)] public partial bool ManualInstall { get; set; }
        [ConfigItem<bool>("HintBuy", false)] public partial bool BuyGame { get; set; }
        [ConfigItem<int>("HintClearRubbish", 0)] public partial int CleanJunkFile { get; set; }
        [ConfigItem<bool>("HintUpdateMod", false)] public partial bool UpdateMod { get; set; }
        [ConfigItem<bool>("HintCustomCommand", false)] public partial bool HomepageCommand { get; set; }
        [ConfigItem<bool>("HintCustomWarn", false)] public partial bool UntrustedHomepage { get; set; }
        [ConfigItem<bool>("HintMoreAdvancedSetup", false)] public partial bool MoreInstanceSetup { get; set; }
        [ConfigItem<bool>("HintIndieSetup", false)] public partial bool IndieSetup { get; set; }
        [ConfigItem<bool>("HintProfileSelect", false)] public partial bool LaunchWithProfile { get; set; }
        [ConfigItem<bool>("HintExportConfig", false)] public partial bool ExportConfig { get; set; }
        [ConfigItem<bool>("HintMaxLog", false)] public partial bool MaxGameLog { get; set; }
        [ConfigItem<bool>("HintDisableGamePathCheckTip", false)] public partial bool NonAsciiGamePath { get; set; }
        [ConfigItem<bool>("UiLauncherCEHint", false)] public partial bool CEMessage { get; set; }
        [ConfigItem<int>("UiLauncherCEHintCount", 0)] public partial int CEMessageCount { get; set; }
        [ConfigItem<bool>("UiSchematicFirstTimeHintShown", false)] public partial bool SchematicFirstTime { get; set; }
    }

    [ConfigGroup("System")] partial class SystemConfigGroup
    {
        [ConfigItem<bool>("SystemEula", false)] public partial bool LauncherEula { get; set; }
        [ConfigItem<int>("SystemCount", 0, ConfigSource.SharedEncrypt)] public partial int StartupCount { get; set; }
        [ConfigItem<int>("SystemLaunchCount", 0, ConfigSource.SharedEncrypt)] public partial int LaunchCount { get; set; }
        [ConfigItem<int>("SystemLastVersionReg", 0, ConfigSource.SharedEncrypt)] public partial int LastVersion { get; set; }
        [ConfigItem<int>("SystemHighestSavedBetaVersionReg", 0, ConfigSource.SharedEncrypt)] public partial int LastSavedBetaVersion { get; set; }
        [ConfigItem<int>("SystemHighestBetaVersionReg", 0, ConfigSource.SharedEncrypt)] public partial int LastBetaVersion { get; set; }
        [ConfigItem<int>("SystemHighestAlphaVersionReg", 0, ConfigSource.SharedEncrypt)] public partial int LastAlphaVersion { get; set; }
        [ConfigItem<int>("SystemSetupVersionReg", 1)] public partial int SetupVersionGlobal { get; set; }
        [ConfigItem<int>("SystemSetupVersionIni", 1, ConfigSource.Local)] public partial int SetupVersionLocal { get; set; }
        [ConfigItem<string>("SystemSystemCache", "")] public partial string CacheDirectory { get; set; }
        [ConfigItem<int>("SystemSystemUpdate", 0, ConfigSource.Local)] public partial int UpdateSolution { get; set; }
        [ConfigItem<int>("SystemSystemUpdateBranch", 0, ConfigSource.Local)] public partial int UpdateBranch { get; set; }
        [ConfigItem<int>("SystemSystemActivity", 0, ConfigSource.Local)] public partial int AnnounceSolution { get; set; }
        [ConfigItem<string>("SystemSystemAnnouncement", "", ConfigSource.Local)] public partial string ShowedAnnouncements { get; set; }
        [ConfigItem<bool>("SystemDisableHardwareAcceleration", false)] public partial bool DisableHardwareAcceleration { get; set; }
        [ConfigItem<bool>("SystemTelemetry", false)] public partial bool Telemetry { get; set; }
        [ConfigItem<string>("SystemMirrorChyanKey", "", ConfigSource.SharedEncrypt)] public partial string MirrorChyanKey { get; set; }
        [ConfigItem<int>("SystemMaxLog", 13)] public partial int MaxGameLog { get; set; }
        [ConfigItem<string>("LaunchUuid", "")] public partial string LaunchUuid { get; set; }
        [ConfigItem<string>("LoginLegacyName", "", ConfigSource.SharedEncrypt)] public partial string LoginLegacyName { get; set; }
        [ConfigItem<string>("LoginMsJson", "{}", ConfigSource.SharedEncrypt)] public partial string LoginMsJson { get; set; }
        [ConfigItem<int>("LoginMsAuthType", 1)] public partial int LoginMsAuthType { get; set; }

        [ConfigGroup("HttpProxy")] partial class HttpProxyConfigGroup
        {
            [ConfigItem<string>("SystemHttpProxy", "", ConfigSource.SharedEncrypt)] public partial string IsEnabled { get; set; }
            [ConfigItem<int>("SystemHttpProxyType", 1)] public partial int Type { get; set; }
            [ConfigItem<string>("SystemHttpProxyCustomUsername", "")] public partial string CustomUsername { get; set; }
            [ConfigItem<string>("SystemHttpProxyCustomPassword", "")] public partial string CustomPassword { get; set; }
        }

        [ConfigGroup("Debug")] partial class DebugConfigGroup
        {
            [ConfigItem<bool>("SystemDebugMode", false)] public partial bool Enabled { get; set; }
            [ConfigItem<int>("SystemDebugAnim", 9)] public partial int AnimationSpeed { get; set; }
            [ConfigItem<bool>("SystemDebugDelay", false)] public partial bool AddRandomDelay { get; set; }
            [ConfigItem<bool>("SystemDebugSkipCopy", false)] public partial bool DontCopy { get; set; }
        }
    }

    [ConfigGroup("Cache")] partial class CacheConfigGroup
    {
        [ConfigItem<string>("CacheExportConfig", "")] public partial string ExportConfigPath { get; set; }
        [ConfigItem<string>("CacheSavedPageUrl", "")] public partial string SavedHomepageUrl { get; set; }
        [ConfigItem<string>("CacheSavedPageVersion", "")] public partial string SavedHomepageVersion { get; set; }
        [ConfigItem<string>("CacheDownloadFolder", "")] public partial string FileDownloadFolder { get; set; }
        [ConfigItem<string>("ToolDownloadCustomUserAgent", "")] public partial string DownloadUserAgent { get; set; }
        [ConfigItem<int>("CacheJavaListVersion", 0)] public partial int JavaListVersion { get; set; }
        [ConfigItem<string>("CacheAuthUuid", "", ConfigSource.SharedEncrypt)] public partial string AuthUuid { get; set; }
        [ConfigItem<string>("CacheAuthName", "", ConfigSource.SharedEncrypt)] public partial string AuthUserName { get; set; }
        [ConfigItem<string>("CacheAuthUsername", "", ConfigSource.SharedEncrypt)] public partial string AuthThirdPartyUserName { get; set; }
        [ConfigItem<string>("CacheAuthPass", "", ConfigSource.SharedEncrypt)] public partial string AuthPassword { get; set; }
        [ConfigItem<string>("CacheAuthServerServer", "", ConfigSource.SharedEncrypt)] public partial string AuthServerAddress { get; set; }
    }

    [ConfigGroup("Link")] partial class LinkConfigGroup
    {
        [ConfigItem<bool>("LinkEula", false)] public partial bool LinkEula { get; set; }
        [ConfigItem<string>("LinkUsername", "")] public partial string Username { get; set; }
        [ConfigItem<string>("LinkAnnounceCache", "", ConfigSource.SharedEncrypt)] public partial string AnnounceCache { get; set; }
        [ConfigItem<int>("LinkAnnounceCacheVer", 0)] public partial int AnnounceCacheVer { get; set; }
        [ConfigItem<int>("LinkRelayType", 0)] public partial int RelayType { get; set; }
        [ConfigItem<int>("LinkServerType", 1)] public partial int ServerType { get; set; }
        [ConfigItem<bool>("LinkLatencyFirstMode", true)] public partial bool LatencyFirstMode { get; set; }
        [ConfigItem<string>("LinkRelayServer", "")] public partial string RelayServer { get; set; }
        [ConfigItem<string>("LinkNaidRefreshToken", "", ConfigSource.SharedEncrypt)] public partial string NaidRefreshToken { get; set; }
        [ConfigItem<string>("LinkNaidRefreshExpiresAt", "", ConfigSource.SharedEncrypt)] public partial string NaidRefreshExpireTime { get; set; }
        [ConfigItem<bool>("LinkFirstTimeNetTest", true, ConfigSource.SharedEncrypt)] public partial bool DoFirstTimeNetTest { get; set; }
    }

    [ConfigGroup("Tool")] partial class ToolConfigGroup
    {
        [ConfigItem<string>("CompFavorites", "[]")] public partial string CompFavorites { get; set; }
        [ConfigItem<bool>("ToolFixAuthlib", true)] public partial bool FixAuthLib { get; set; }
        [ConfigItem<bool>("ToolHelpChinese", true)] public partial bool AutoChangeLanguage { get; set; }

        [ConfigGroup("Download")] partial class DownloadConfigGroup
        {
            [ConfigItem<int>("ToolDownloadThread", 63)] public partial int ThreadLimit { get; set; }
            [ConfigItem<int>("ToolDownloadSpeed", 42)] public partial int SpeedLimit { get; set; }
            [ConfigItem<int>("ToolDownloadSource", 1)] public partial int FileSourceSolution { get; set; }
            [ConfigItem<int>("ToolDownloadVersion", 1)] public partial int VersionSourceSolution { get; set; }
            [ConfigItem<int>("ToolDownloadTranslate", 0)] public partial int NameFormatV1 { get; set; }
            [ConfigItem<int>("ToolDownloadTranslateV2", 1)] public partial int NameFormatV2 { get; set; }
            [ConfigItem<bool>("ToolDownloadIgnoreQuilt", false)] public partial bool UiIgnoreQuilt { get; set; }
            [ConfigItem<bool>("ToolDownloadClipboard", false)] public partial bool ListenClipboard { get; set; }
            [ConfigItem<int>("ToolDownloadMod", 1)] public partial int CompSourceSolution { get; set; }
            [ConfigItem<int>("ToolModLocalNameStyle", 0)] public partial int UiCompNameSolution { get; set; }
            [ConfigItem<bool>("ToolDownloadAutoSelectVersion", true)] public partial bool AutoSelectInstance { get; set; }
        }

        [ConfigGroup("Update")] partial class UpdateConfigGroup
        {
            [ConfigItem<int>("ToolUpdateAlpha", 0, ConfigSource.SharedEncrypt)] public partial int Alpha { get; set; }
            [ConfigItem<bool>("ToolUpdateRelease", false)] public partial bool Release { get; set; }
            [ConfigItem<bool>("ToolUpdateSnapshot", false)] public partial bool Snapshot { get; set; }
            [ConfigItem<string>("ToolUpdateReleaseLast", "")] public partial string LastRelease { get; set; }
            [ConfigItem<string>("ToolUpdateSnapshotLast", "")] public partial string LastSnapshot { get; set; }
        }
    }

    [ConfigGroup("UI")] partial class UiConfigGroup
    {
        [ConfigItem<double>("WindowHeight", 550, ConfigSource.Local)] public partial double WindowHeight { get; set; }
        [ConfigItem<double>("WindowWidth", 900, ConfigSource.Local)] public partial double WindowWidth { get; set; }
        [ConfigItem<bool>("UiLauncherLogo", true, ConfigSource.Local)] public partial bool ShowStartupLogo { get; set; }

        [ConfigGroup("Theme")] partial class ThemeConfigGroup
        {
            [ConfigItem<int>("UiDarkMode", 2)] public partial int ColorMode { get; set; }
            [ConfigItem<int>("UiDarkColor", 1)] public partial int DarkColor { get; set; }
            [ConfigItem<int>("UiLightColor", 1)] public partial int LightColor { get; set; }
            [ConfigItem<int>("UiLauncherTransparent", 600, ConfigSource.Local)] public partial int WindowOpacity { get; set; }
            [ConfigItem<int>("UiLauncherHue", 180, ConfigSource.Local)] public partial int WindowHue { get; set; }
            [ConfigItem<int>("UiLauncherSat", 80, ConfigSource.Local)] public partial int WindowSat { get; set; }
            [ConfigItem<int>("UiLauncherDelta", 90, ConfigSource.Local)] public partial int WindowDelta { get; set; }
            [ConfigItem<int>("UiLauncherLight", 20, ConfigSource.Local)] public partial int WindowLight { get; set; }
            [ConfigItem<int>("UiLauncherTheme", 0, ConfigSource.Local)] public partial int ThemeSelected { get; set; }
            [ConfigItem<string>("UiLauncherThemeGold", "")] public partial string ThemeGoldCode { get; set; }
            [ConfigItem<string>("UiLauncherThemeHide", "0|1|2|3|4")] public partial string ThemeHiddenV1 { get; set; }
            [ConfigItem<string>("UiLauncherThemeHide2", "0|1|2|3|4")] public partial string ThemeHiddenV2 { get; set; }
        }

        [ConfigGroup("Background")] partial class BackgroundConfigGroup
        {
            [ConfigItem<bool>("UiBackgroundColorful", true, ConfigSource.Local)] public partial bool BackgroundColorful { get; set; }
            [ConfigItem<int>("UiBackgroundOpacity", 1000, ConfigSource.Local)] public partial int WallpaperOpacity { get; set; }
            [ConfigItem<int>("UiBackgroundBlur", 0, ConfigSource.Local)] public partial int WallpaperBlurRadius { get; set; }
            [ConfigItem<int>("UiBackgroundSuit", 0, ConfigSource.Local)] public partial int WallpaperSuitMode { get; set; }
        }

        [ConfigGroup("Blur")] partial class BlurConfigGroup
        {
            [ConfigItem<bool>("UiBlur", false, ConfigSource.Local)] public partial bool IsEnabled { get; set; }
            [ConfigItem<int>("UiBlurValue", 16, ConfigSource.Local)] public partial int Radius { get; set; }
            [ConfigItem<int>("UiBlurSamplingRate", 70, ConfigSource.Local)] public partial int SamplingRate { get; set; }
            [ConfigItem<int>("UiBlurType", 0, ConfigSource.Local)] public partial int KernelType { get; set; }
        }

        [ConfigGroup("Homepage")] partial class HomepageConfigGroup
        {
            [ConfigItem<int>("UiCustomType", 0, ConfigSource.Local)] public partial int Type { get; set; }
            [ConfigItem<int>("UiCustomPreset", 0, ConfigSource.Local)] public partial int SelectedPreset { get; set; }
            [ConfigItem<string>("UiCustomNet", "", ConfigSource.Local)] public partial string CustomUrl { get; set; }
        }

        [ConfigItem<bool>("UiLockWindowSize", false)] public partial bool LockWindowSize { get; set; }
        [ConfigItem<int>("UiLogoType", 1, ConfigSource.Local)] public partial int LogoSolution { get; set; }
        [ConfigItem<string>("UiLogoText", "", ConfigSource.Local)] public partial string LogoCustomText { get; set; }
        [ConfigItem<bool>("UiLogoLeft", false, ConfigSource.Local)] public partial bool TopBarLeftAlign { get; set; }
        [ConfigItem<int>("UiAniFPS", 59)] public partial int AnimationFpsLimit { get; set; }
        [ConfigItem<string>("UiFont", "", ConfigSource.Local)] public partial string Font { get; set; }
        [ConfigItem<bool>("UiAutoPauseVideo", true, ConfigSource.Local)] public partial bool AutoPauseVideo { get; set; }

        [ConfigGroup("Music")] partial class MusicConfigGroup
        {
            [ConfigItem<int>("UiMusicVolume", 500, ConfigSource.Local)] public partial int Volume { get; set; }
            [ConfigItem<bool>("UiMusicStop", false, ConfigSource.Local)] public partial bool StopInGame { get; set; }
            [ConfigItem<bool>("UiMusicStart", false, ConfigSource.Local)] public partial bool StartInGame { get; set; }
            [ConfigItem<bool>("UiMusicAuto", true, ConfigSource.Local)] public partial bool StartOnStartup { get; set; }
            [ConfigItem<bool>("UiMusicRandom", true, ConfigSource.Local)] public partial bool ShufflePlayback { get; set; }
            [ConfigItem<bool>("UiMusicSMTC", true, ConfigSource.Local)] public partial bool EnableSMTC { get; set; }
        }

        [ConfigGroup("Hide")] partial class HideConfigGroup
        {
            [ConfigItem<bool>("UiHiddenPageDownload", false, ConfigSource.Local)] public partial bool PageDownload { get; set; }
            [ConfigItem<bool>("UiHiddenPageLink", false, ConfigSource.Local)] public partial bool PageLink { get; set; }
            [ConfigItem<bool>("UiHiddenPageSetup", false, ConfigSource.Local)] public partial bool PageSetup { get; set; }
            [ConfigItem<bool>("UiHiddenPageOther", false, ConfigSource.Local)] public partial bool PageOther { get; set; }
            [ConfigItem<bool>("UiHiddenFunctionSelect", false, ConfigSource.Local)] public partial bool FunctionSelect { get; set; }
            [ConfigItem<bool>("UiHiddenFunctionModUpdate", false, ConfigSource.Local)] public partial bool FunctionModUpdate { get; set; }
            [ConfigItem<bool>("UiHiddenFunctionHidden", false, ConfigSource.Local)] public partial bool FunctionHidden { get; set; }
            [ConfigItem<bool>("UiHiddenSetupLaunch", false, ConfigSource.Local)] public partial bool SetupLaunch { get; set; }
            [ConfigItem<bool>("UiHiddenSetupUi", false, ConfigSource.Local)] public partial bool SetupUi { get; set; }
            [ConfigItem<bool>("UiHiddenSetupSystem", false, ConfigSource.Local)] public partial bool SetupSystem { get; set; }
            [ConfigItem<bool>("UiHiddenOtherHelp", false, ConfigSource.Local)] public partial bool OtherHelp { get; set; }
            [ConfigItem<bool>("UiHiddenOtherFeedback", false, ConfigSource.Local)] public partial bool OtherFeedback { get; set; }
            [ConfigItem<bool>("UiHiddenOtherVote", false, ConfigSource.Local)] public partial bool OtherVote { get; set; }
            [ConfigItem<bool>("UiHiddenOtherAbout", false, ConfigSource.Local)] public partial bool OtherAbout { get; set; }
            [ConfigItem<bool>("UiHiddenOtherTest", false, ConfigSource.Local)] public partial bool OtherTest { get; set; }
            [ConfigItem<bool>("UiHiddenVersionEdit", false, ConfigSource.Local)] public partial bool InstanceEdit { get; set; }
            [ConfigItem<bool>("UiHiddenVersionExport", false, ConfigSource.Local)] public partial bool InstanceExport { get; set; }
            [ConfigItem<bool>("UiHiddenVersionSave", false, ConfigSource.Local)] public partial bool InstanceSave { get; set; }
            [ConfigItem<bool>("UiHiddenVersionScreenshot", false, ConfigSource.Local)] public partial bool InstanceScreenshot { get; set; }
            [ConfigItem<bool>("UiHiddenVersionMod", false, ConfigSource.Local)] public partial bool InstanceMod { get; set; }
            [ConfigItem<bool>("UiHiddenVersionResourcePack", false, ConfigSource.Local)] public partial bool InstanceResourcePack { get; set; }
            [ConfigItem<bool>("UiHiddenVersionShader", false, ConfigSource.Local)] public partial bool InstanceShader { get; set; }
            [ConfigItem<bool>("UiHiddenVersionSchematic", false, ConfigSource.Local)] public partial bool InstanceSchematic { get; set; }
        }
    }

    [ConfigGroup("Launch")] partial class LaunchConfigGroup
    {
        [ConfigItem<string>("LaunchInstanceSelect", "", ConfigSource.Local)] public partial string SelectedInstance { get; set; }
        [ConfigItem<string>("LaunchFolderSelect", "", ConfigSource.Local)] public partial string SelectedFolder { get; set; }
        [ConfigItem<string>("LaunchFolders", "")] public partial string Folders { get; set; }
        [ConfigItem<int>("LaunchRamType", 0, ConfigSource.Local)] public partial int MemorySolution { get; set; }
        [ConfigItem<int>("LaunchRamCustom", 15, ConfigSource.Local)] public partial int CustomMemorySize { get; set; }
        [ConfigItem<int>("LaunchPreferredIpStack", 1)] public partial int PreferredIpStack { get; set; }
        [ConfigItem<bool>("LaunchArgumentRam", false)] public partial bool OptimizeMemory { get; set; }
        [ConfigItem<string>("LaunchAdvanceJvm", "-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow -Djdk.lang.Process.allowAmbiguousCommands=true -Dfml.ignoreInvalidMinecraftCertificates=True -Dfml.ignorePatchDiscrepancies=True -Dlog4j2.formatMsgNoLookups=true", ConfigSource.Local)] public partial string JvmArgs { get; set; }
        [ConfigItem<string>("LaunchAdvanceGame", "", ConfigSource.Local)] public partial string GameArgs { get; set; }
        [ConfigItem<string>("LaunchAdvanceRun", "", ConfigSource.Local)] public partial string PreLaunchCommand { get; set; }
        [ConfigItem<bool>("LaunchAdvanceRunWait", true, ConfigSource.Local)] public partial bool PreLaunchCommandWait { get; set; }
        [ConfigItem<bool>("LaunchAdvanceDisableJLW", false, ConfigSource.Local)] public partial bool DisableJlw { get; set; }
        [ConfigItem<bool>("LaunchAdvanceDisableRW", false, ConfigSource.Local)] public partial bool DisableRw { get; set; }
        [ConfigItem<bool>("LaunchAdvanceGraphicCard", true)] public partial bool SetGpuPreference { get; set; }
        [ConfigItem<bool>("LaunchAdvanceNoJavaw", false)] public partial bool DontUseJavaw { get; set; }
        [ConfigItem<string>("LaunchArgumentTitle", "", ConfigSource.Local)] public partial string Title { get; set; }
        [ConfigItem<string>("LaunchArgumentInfo", "PCL", ConfigSource.Local)] public partial string TypeInfo { get; set; }
        [ConfigItem<string>("LaunchArgumentJavaSelect", "")] public partial string SelectedJava { get; set; }
        [ConfigItem<string>("LaunchArgumentJavaUser", "[]")] public partial string Javas { get; set; }
        [ConfigItem<int>("LaunchArgumentIndie", 0, ConfigSource.Local)] public partial int IndieSolutionV1 { get; set; }
        [ConfigItem<int>("LaunchArgumentIndieV2", 4, ConfigSource.Local)] public partial int IndieSolutionV2 { get; set; }
        [ConfigItem<int>("LaunchArgumentVisible", 5)] public partial int LauncherVisibility { get; set; }
        [ConfigItem<int>("LaunchArgumentPriority", 1)] public partial int ProcessPriority { get; set; }
        [ConfigItem<int>("LaunchArgumentWindowWidth", 854, ConfigSource.Local)] public partial int WindowWidthLaunch { get; set; }
        [ConfigItem<int>("LaunchArgumentWindowHeight", 480, ConfigSource.Local)] public partial int WindowHeightLaunch { get; set; }
        [ConfigItem<int>("LaunchArgumentWindowType", 1, ConfigSource.Local)] public partial int WindowType { get; set; }
    }

    [ConfigGroup("Instance")] partial class InstanceConfigGroup
    {
        [ConfigItem<string>("VersionAdvanceJvm", "", ConfigSource.GameInstance)] public partial ArgConfig<string> JvmArgs { get; }
        [ConfigItem<string>("VersionAdvanceGame", "", ConfigSource.GameInstance)] public partial ArgConfig<string> GameArgs { get; }
        [ConfigItem<int>("VersionAdvanceRenderer", 0, ConfigSource.GameInstance)] public partial ArgConfig<int> Renderer { get; }
        [ConfigItem<int>("VersionAdvanceAssets", 0, ConfigSource.GameInstance)] public partial ArgConfig<int> AssetVerifySolutionV1 { get; }
        [ConfigItem<bool>("VersionAdvanceAssetsV2", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> DisableAssetVerifyV2 { get; }
        [ConfigItem<bool>("VersionAdvanceJava", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> IgnoreJavaCompatibility { get; }
        [ConfigItem<bool>("VersionAdvanceDisableJlw", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> DisableJlwObsolete { get; }
        [ConfigItem<string>("VersionAdvanceRun", "", ConfigSource.GameInstance)] public partial ArgConfig<string> PreLaunchCommand { get; }
        [ConfigItem<bool>("VersionAdvanceRunWait", true, ConfigSource.GameInstance)] public partial ArgConfig<bool> PreLaunchCommandWait { get; }
        [ConfigItem<bool>("VersionAdvanceDisableJLW", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> DisableJlw { get; }
        [ConfigItem<bool>("VersionAdvanceUseProxyV2", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> UseProxy { get; }
        [ConfigItem<bool>("VersionAdvanceDisableRW", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> DisableRw { get; }
        [ConfigItem<int>("VersionRamType", 2, ConfigSource.GameInstance)] public partial ArgConfig<int> MemorySolution { get; }
        [ConfigItem<int>("VersionRamCustom", 15, ConfigSource.GameInstance)] public partial ArgConfig<int> CustomMemorySize { get; }
        [ConfigItem<int>("VersionRamOptimize", 0, ConfigSource.GameInstance)] public partial ArgConfig<int> OptimizeMemoryResolution { get; }
        [ConfigItem<string>("VersionArgumentTitle", "", ConfigSource.GameInstance)] public partial ArgConfig<string> Title { get; }
        [ConfigItem<bool>("VersionArgumentTitleEmpty", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> UseGlobalTitle { get; }
        [ConfigItem<string>("VersionArgumentInfo", "", ConfigSource.GameInstance)] public partial ArgConfig<string> TypeInfo { get; }
        [ConfigItem<int>("VersionArgumentIndie", -1, ConfigSource.GameInstance)] public partial ArgConfig<int> IndieV1 { get; }
        [ConfigItem<bool>("VersionArgumentIndieV2", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> IndieV2 { get; }
        [ConfigItem<string>("VersionArgumentJavaSelect", "使用全局设置", ConfigSource.GameInstance)] public partial ArgConfig<string> SelectedJava { get; }
        [ConfigItem<string>("VersionServerEnter", "", ConfigSource.GameInstance)] public partial ArgConfig<string> ServerToEnter { get; }
        [ConfigItem<int>("VersionServerLoginRequire", 0, ConfigSource.GameInstance)] public partial ArgConfig<int> LoginRequirementSolution { get; }
        [ConfigItem<string>("VersionServerAuthRegister", "", ConfigSource.GameInstance)] public partial ArgConfig<string> AuthRegisterAddress { get; }
        [ConfigItem<string>("VersionServerAuthName", "", ConfigSource.GameInstance)] public partial ArgConfig<string> AuthServerDisplayName { get; }
        [ConfigItem<string>("VersionServerAuthServer", "", ConfigSource.GameInstance)] public partial ArgConfig<string> AuthServerAddress { get; }
        [ConfigItem<bool>("VersionServerLoginLock", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> AuthTypeLucked { get; }
        [ConfigItem<int>("VersionLaunchCount", 0, ConfigSource.GameInstance)] public partial ArgConfig<int> LaunchCount { get; }
        [ConfigItem<bool>("IsStar", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> Starred { get; }
        [ConfigItem<int>("DisplayType", 0, ConfigSource.GameInstance)] public partial ArgConfig<int> DisplayType { get; }
        [ConfigItem<string>("Logo", "", ConfigSource.GameInstance)] public partial ArgConfig<string> LogoPath { get; }
        [ConfigItem<bool>("LogoCustom", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> IsLogoCustom { get; }
        [ConfigItem<string>("CustomInfo", "", ConfigSource.GameInstance)] public partial ArgConfig<string> CustomInfo { get; }
        [ConfigItem<string>("Info", "", ConfigSource.GameInstance)] public partial ArgConfig<string> Info { get; }
        [ConfigItem<string>("ReleaseTime", "", ConfigSource.GameInstance)] public partial ArgConfig<string> ReleaseTime { get; }
        [ConfigItem<int>("State", 0, ConfigSource.GameInstance)] public partial ArgConfig<int> State { get; }
        [ConfigItem<string>("VersionFabric", "", ConfigSource.GameInstance)] public partial ArgConfig<string> FabricVersion { get; }
        [ConfigItem<string>("VersionLegacyFabric", "", ConfigSource.GameInstance)] public partial ArgConfig<string> LegacyFabricVersion { get; }
        [ConfigItem<string>("VersionQuilt", "", ConfigSource.GameInstance)] public partial ArgConfig<string> QuiltVersion { get; }
        [ConfigItem<string>("VersionLabyMod", "", ConfigSource.GameInstance)] public partial ArgConfig<string> LabyModVersion { get; }
        [ConfigItem<string>("VersionOptiFine", "", ConfigSource.GameInstance)] public partial ArgConfig<string> OptiFineVersion { get; }
        [ConfigItem<bool>("VersionLiteLoader", false, ConfigSource.GameInstance)] public partial ArgConfig<bool> HasLiteLoader { get; }
        [ConfigItem<string>("VersionForge", "", ConfigSource.GameInstance)] public partial ArgConfig<string> ForgeVersion { get; }
        [ConfigItem<string>("VersionNeoForge", "", ConfigSource.GameInstance)] public partial ArgConfig<string> NeoForgeVersion { get; }
        [ConfigItem<string>("VersionCleanroom", "", ConfigSource.GameInstance)] public partial ArgConfig<string> CleanroomVersion { get; }
        [ConfigItem<int>("VersionApiCode", -1, ConfigSource.GameInstance)] public partial ArgConfig<int> SortCode { get; }
        [ConfigItem<string>("VersionOriginal", "Unknown", ConfigSource.GameInstance)] public partial ArgConfig<string> McVersion { get; }
        [ConfigItem<int>("VersionOriginalMain", -1, ConfigSource.GameInstance)] public partial ArgConfig<int> VersionMajor { get; }
        [ConfigItem<int>("VersionOriginalSub", -1, ConfigSource.GameInstance)] public partial ArgConfig<int> VersionMinor { get; }
        [ConfigItem<string>("VersionModpackVersion", "", ConfigSource.GameInstance)] public partial ArgConfig<string> ModpackVersion { get; }
        [ConfigItem<string>("VersionModpackSource", "", ConfigSource.GameInstance)] public partial ArgConfig<string> ModpackSource { get; }
        [ConfigItem<string>("VersionModpackId", "", ConfigSource.GameInstance)] public partial ArgConfig<string> ModpackId { get; }
    }
}
