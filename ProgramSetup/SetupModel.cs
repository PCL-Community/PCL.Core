using System;
using System.Collections.Generic;
using static PCL.Core.ProgramSetup.SetupEntrySource;

namespace PCL.Core.ProgramSetup;

/// <summary>
/// 设置模型，完全没有使用的设置项会被标记为 Obsolete（需要从旧版迁移至新版的不会被标记）
/// </summary>
public sealed class SetupModel
{
    public const int SetupVersionNum = 1;

    private readonly IReadOnlyDictionary<string, object> _setupPathMap;

    public readonly Identifications Identification = new();
    public readonly Counters Counter = new();
    public readonly Hints Hint = new();
    public readonly Caches Cache = new();
    public readonly UiSettings Ui = new();
    public readonly SystemSettings System = new();
    public readonly ToolSettings Tool = new();
    public readonly LaunchSettings Launch = new();
    public readonly McInstanceSettings Minecraft = new();

    public SetupModel()
    {
        _setupPathMap = new Dictionary<string, object>
        {
            ["Identification"] = Identification,
            ["Counter"] = Counter,
            ["Hint"] = Hint,
            ["Cache"] = Cache,
            ["Ui"] = Ui,
            ["Ui.Hide"] = Ui.Hide,
            ["Ui.Music"] = Ui.Music,
            ["Ui.Background"] = Ui.Background,
            ["Ui.MainPage"] = Ui.MainPage,
            ["System"] = System,
            ["System.Debug"] = System.Debug,
            ["System.Link"] = System.Link,
            ["System.Login"] = System.Login,
            ["Tool"] = Tool,
            ["Tool.Download"] = Tool.Download,
            ["Launch"] = Launch,
            ["Minecraft"] = Minecraft
        };
    }
    
    public ISetupEntry? GetEntryFromPath(string path)
    {
        try
        {
            var index = path.LastIndexOf('.');
            var owner = _setupPathMap[path[..index]];
            var result = owner.GetType().GetField(path[(index + 1)..]).GetValue(owner);
            return result as ISetupEntry;
        }
        catch
        {
            return null;
        }
    }

    public sealed class Identifications
    {
        [Obsolete] public readonly SetupEntry<string> Identifier = new("Identify", SystemGlobal);
        [Obsolete] public readonly SetupEntry<int> GlobalVersion = new("SystemSetupVersionReg", SetupVersionNum, SystemGlobal);
        [Obsolete] public readonly SetupEntry<int> LocalVersion = new("SystemSetupVersionIni", SetupVersionNum, PathLocal);
        [Obsolete] public readonly SetupEntry<int> JavaListVersion = new("CacheJavaListVersion", SystemGlobal);
    }

    public sealed class Counters
    {
        public readonly SetupEntry<int> LauncherStartCount = new("SystemCount", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> GameLaunchCount = new("SystemLaunchCount", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> LastVersion = new("SystemLastVersionReg", SystemGlobal, isEncrypted: true);
        [Obsolete] public readonly SetupEntry<int> LastSavedBetaVersion = new("SystemHighestSavedBetaVersionReg", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> LastBetaVersion = new("SystemHighestBetaVersionReg", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> LastAlphaVersion = new("SystemHighestAlphaVersionReg", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> DisplayedAnnouncements = new("SystemSystemAnnouncement", SystemGlobal);
        public readonly SetupEntry<string> LastMcRelease = new("ToolUpdateReleaseLast", SystemGlobal);
        public readonly SetupEntry<string> LastMcSnapshot = new("ToolUpdateSnapshotLast", SystemGlobal);
    }

    public sealed class Hints
    {
        public readonly SetupEntry<bool> DownloadThreadCount = new("HintDownloadThread", SystemGlobal);
        [Obsolete] public readonly SetupEntry<int> Notification = new("HintNotice", SystemGlobal);
        public readonly SetupEntry<int> DownloadComp = new("HintDownload", SystemGlobal);
        public readonly SetupEntry<bool> InstallPageBack = new("HintInstallBack", SystemGlobal);
        public readonly SetupEntry<bool> HideInstance = new("HintHide", SystemGlobal);
        public readonly SetupEntry<bool> ManualInstallation = new("HintHandInstall", SystemGlobal);
        public readonly SetupEntry<bool> BuyMinecraft = new("HintBuy", SystemGlobal);
        public readonly SetupEntry<int> ClearJunkFiles = new("HintClearRubbish", SystemGlobal);
        public readonly SetupEntry<bool> UpdateMod = new("HintUpdateMod", SystemGlobal);
        public readonly SetupEntry<bool> MainPage = new("HintCustomWarn", SystemGlobal);
        public readonly SetupEntry<bool> MainPageCommand = new("HintCustomCommand", SystemGlobal);
        public readonly SetupEntry<bool> MoreAdvancedSettings = new("HintMoreAdvancedSetup", SystemGlobal);
        public readonly SetupEntry<bool> IndieSettings = new("HintIndieSetup", SystemGlobal);
        public readonly SetupEntry<bool> ProfileSelection = new("HintProfileSelect", SystemGlobal);
        public readonly SetupEntry<bool> ExportConfiguration = new("HintExportConfig", SystemGlobal);
        public readonly SetupEntry<bool> MaxLogLines = new("HintMaxLog", SystemGlobal);
        public readonly SetupEntry<bool> NonAsciiGamePath = new("HintDisableGamePathCheckTip", SystemGlobal);
        public readonly SetupEntry<bool> LauncherEula = new("SystemEula", SystemGlobal);
        public readonly SetupEntry<bool> CommunityEdition = new("UiLauncherCEHint", true, SystemGlobal);
    }

    public sealed class Caches
    {
        public readonly SetupEntry<string> ExportConfigPath = new("CacheExportConfig", SystemGlobal);
        public readonly SetupEntry<string> MainPageUrl = new("CacheSavedPageUrl", SystemGlobal);
        public readonly SetupEntry<string> MainPageVersion = new("CacheSavedPageVersion", SystemGlobal);
        public readonly SetupEntry<string> JavaPaths = new("LaunchArgumentJavaUser", "[]", SystemGlobal);
        public readonly SetupEntry<string> AuthUuid = new("CacheAuthUuid", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> AuthUserName = new("CacheAuthName", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> AuthThirdPartyName = new("CacheAuthUsername", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> AuthPassword = new("CacheAuthPass", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> AuthServerUrl = new("CacheAuthServerServer", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> LinkAnnounce = new("LinkAnnounceCache", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> LinkAnnounceVersion = new("LinkAnnounceCacheVer", SystemGlobal);
        public readonly SetupEntry<string> LinkLastTestDate = new("LinkLastTestDate", SystemGlobal, isEncrypted: true);
    }

    public sealed class UiSettings
    {
        public readonly UiHideSettings Hide = new();
        public readonly MusicSettings Music = new();
        public readonly BackgroundSettings Background = new();
        public readonly MainPageSettings MainPage = new();

        public readonly SetupEntry<int> DarkMode = new("UiDarkMode", 2, SystemGlobal);
        public readonly SetupEntry<int> AnimationFpsLimit = new("UiAniFPS", 59, SystemGlobal);
        public readonly SetupEntry<int> DarkModeColor = new("UiDarkColor", 1, SystemGlobal);
        public readonly SetupEntry<int> LightModeColor = new("UiLightColor", 1, SystemGlobal);

        public readonly SetupEntry<int> WindowHeight = new("WindowHeight", 550, PathLocal);
        public readonly SetupEntry<int> WindowWidth = new("WindowWidth", 900, PathLocal);
        public readonly SetupEntry<int> Transparency = new("UiLauncherTransparent", 600, PathLocal);
        public readonly SetupEntry<int> ColorHue = new("UiLauncherHue", 180, PathLocal);
        public readonly SetupEntry<int> ColorSat = new("UiLauncherSat", 80, PathLocal);
        public readonly SetupEntry<int> ColorDelta = new("UiLauncherDelta", 90, PathLocal);
        public readonly SetupEntry<int> ColorLight = new("UiLauncherLight", 20, PathLocal);
        public readonly SetupEntry<int> Theme = new("UiLauncherTheme", PathLocal);
        public readonly SetupEntry<string> ThemeGold = new("UiLauncherThemeGold", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> HiddenThemesV1 = new("UiLauncherThemeHide", "0|1|2|3|4", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> HiddenThemesV2 = new("UiLauncherThemeHide2", "0|1|2|3|4", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<bool> ShowLogoOnStartup = new("UiLauncherLogo", true, PathLocal);
        public readonly SetupEntry<int> LogoResolution = new("UiLogoType", 1, PathLocal);
        public readonly SetupEntry<string> CustomLogoText = new("UiLogoText", PathLocal);
        public readonly SetupEntry<bool> LeftAlignTitle = new("UiLogoLeft", PathLocal);
        public readonly SetupEntry<string> Font = new("UiFont", PathLocal);

        public sealed class UiHideSettings
        {
            public readonly SetupEntry<bool> PageDownload = new("UiHiddenPageDownload", PathLocal);
            public readonly SetupEntry<bool> PageLink = new("UiHiddenPageLink", PathLocal);
            public readonly SetupEntry<bool> PageSetup = new("UiHiddenPageSetup", PathLocal);
            public readonly SetupEntry<bool> PageOther = new("UiHiddenPageOther", PathLocal);
            public readonly SetupEntry<bool> FunctionSelect = new("UiHiddenFunctionSelect", PathLocal);
            public readonly SetupEntry<bool> FunctionModUpdate = new("UiHiddenFunctionModUpdate", PathLocal);
            public readonly SetupEntry<bool> FunctionHidden = new("UiHiddenFunctionHidden", PathLocal);
            public readonly SetupEntry<bool> SetupLaunch = new("UiHiddenSetupLaunch", PathLocal);
            public readonly SetupEntry<bool> SetupUi = new("UiHiddenSetupUi", PathLocal);
            public readonly SetupEntry<bool> SetupLink = new("UiHiddenSetupLink", PathLocal);
            public readonly SetupEntry<bool> SetupSystem = new("UiHiddenSetupSystem", PathLocal);
            public readonly SetupEntry<bool> OtherHelp = new("UiHiddenOtherHelp", PathLocal);
            public readonly SetupEntry<bool> OtherFeedback = new("UiHiddenOtherFeedback", PathLocal);
            public readonly SetupEntry<bool> OtherVote = new("UiHiddenOtherVote", PathLocal);
            public readonly SetupEntry<bool> OtherAbout = new("UiHiddenOtherAbout", PathLocal);
            public readonly SetupEntry<bool> OtherTest = new("UiHiddenOtherTest", PathLocal);
            public readonly SetupEntry<bool> VersionEdit = new("UiHiddenVersionEdit", PathLocal);
            public readonly SetupEntry<bool> VersionExport = new("UiHiddenVersionExport", PathLocal);
            public readonly SetupEntry<bool> VersionSave = new("UiHiddenVersionSave", PathLocal);
            public readonly SetupEntry<bool> VersionScreenshot = new("UiHiddenVersionScreenshot", PathLocal);
            public readonly SetupEntry<bool> VersionMod = new("UiHiddenVersionMod", PathLocal);
            public readonly SetupEntry<bool> VersionResourcePack = new("UiHiddenVersionResourcePack", PathLocal);
            public readonly SetupEntry<bool> VersionShader = new("UiHiddenVersionShader", PathLocal);
            public readonly SetupEntry<bool> VersionSchematic = new("UiHiddenVersionSchematic", PathLocal);
        }

        public sealed class MusicSettings
        {
            public readonly SetupEntry<int> Volume = new("UiMusicVolume", 500, PathLocal);
            public readonly SetupEntry<bool> PauseDuringGame = new("UiMusicStop", PathLocal);
            public readonly SetupEntry<bool> PlayDuringGame = new("UiMusicStart", PathLocal);
            public readonly SetupEntry<bool> ShufflePlayback = new("UiMusicRandom", true, PathLocal);
            public readonly SetupEntry<bool> EnableSmtc = new("UiMusicSMTC", true, PathLocal);
            public readonly SetupEntry<bool> AutoPlayOnStartup = new("UiMusicAuto", true, PathLocal);
        }

        public sealed class BackgroundSettings
        {
            public readonly SetupEntry<bool> ColorfulBackground = new("UiBackgroundColorful", true, PathLocal);
            public readonly SetupEntry<int> Opacity = new("UiBackgroundOpacity", 1000, PathLocal);
            public readonly SetupEntry<int> BlurRadius = new("UiBackgroundBlur", PathLocal);
            public readonly SetupEntry<int> SuitResolution = new("UiBackgroundSuit", PathLocal);
            public readonly SetupEntry<bool> AdvanceBlur = new("UiBlur", PathLocal);
            public readonly SetupEntry<int> AdvanceBlurValue = new("UiBlurValue", 16, PathLocal);
        }

        public sealed class MainPageSettings
        {
            public readonly SetupEntry<int> Resolution = new("UiCustomType", PathLocal);
            public readonly SetupEntry<int> SelectedPreset = new("UiCustomPreset", PathLocal);
            public readonly SetupEntry<string> CustomUrl = new("UiCustomNet", PathLocal);
        }
    }

    public sealed class SystemSettings
    {
        public readonly DebugSettings Debug = new();
        public readonly LinkSettings Link = new();
        public readonly LoginSettings Login = new();

        public readonly SetupEntry<string> CacheFolder = new("SystemSystemCache", SystemGlobal);
        public readonly SetupEntry<string> DownloadFolder = new("CacheDownloadFolder", SystemGlobal);
        public readonly SetupEntry<string> HttpProxy = new("SystemHttpProxy", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<bool> UseSystemProxy = new("SystemUseDefaultProxy", true, SystemGlobal);
        public readonly SetupEntry<bool> DisableHardwareAcceleration = new("SystemDisableHardwareAcceleration", SystemGlobal);
        public readonly SetupEntry<string> TelemetryResolution = new("SystemTelemetry", SystemGlobal); // True or False
        public readonly SetupEntry<string> MirrorChyanKey = new("SystemMirrorChyanKey", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> MaxLogLines = new("SystemMaxLog", 13, SystemGlobal);
        public readonly SetupEntry<int> UpdateResolution = new("SystemSystemUpdate", PathLocal);
        public readonly SetupEntry<int> UpdateBranchResolution = new("SystemSystemUpdateBranch", PathLocal);
        public readonly SetupEntry<int> ActivityResolution = new("SystemSystemActivity", PathLocal);

        public sealed class DebugSettings
        {
            public readonly SetupEntry<bool> Enabled = new("SystemDebugMode", SystemGlobal);
            public readonly SetupEntry<int> AnimationSpeed = new("SystemDebugAnim", 9, SystemGlobal);
            public readonly SetupEntry<bool> AddRandomDelay = new("SystemDebugDelay", SystemGlobal);
            public readonly SetupEntry<bool> SkipExistingFileCopy = new("SystemDebugSkipCopy", SystemGlobal);
        }

        public sealed class LinkSettings
        {
            public readonly SetupEntry<bool> EulaAgreed = new("LinkEula", SystemGlobal);
            public readonly SetupEntry<bool> IsAvailable = new("LinkAvailable", SystemGlobal, isEncrypted: true);
            public readonly SetupEntry<int> RelayType = new("LinkRelayType", SystemGlobal);
            public readonly SetupEntry<int> ServerType = new("LinkServerType", SystemGlobal);
            public readonly SetupEntry<string> RelayServer = new("LinkRelayServer", SystemGlobal);
            public readonly SetupEntry<string> NaidRefreshToken = new("LinkNaidRefreshToken", SystemGlobal, isEncrypted: true);
            public readonly SetupEntry<string> NaidRefreshExpiresAt = new("LinkNaidRefreshExpiresAt", SystemGlobal, isEncrypted: true);
            public readonly SetupEntry<bool> DoFirstTimeNetTest = new("LinkFirstTimeNetTest", true, SystemGlobal);
        }

        public sealed class LoginSettings
        {
            public readonly SetupEntry<string> LegacyName = new("LoginLegacyName", SystemGlobal, isEncrypted: true);
            public readonly SetupEntry<string> MsAuthJson = new("LoginMsJson", "{}", SystemGlobal, isEncrypted: true);
            public readonly SetupEntry<string> MsAuthType = new("LoginMsAuthType", "0", SystemGlobal);
        }
    }

    public sealed class ToolSettings
    {
        public readonly DownloadSettings Download = new();

        public readonly SetupEntry<bool> FixAuthLib = new("ToolFixAuthlib", true, SystemGlobal);
        public readonly SetupEntry<bool> SetMcLanguage = new("ToolHelpChinese", true, SystemGlobal);
        public readonly SetupEntry<int> LocalModNameDisplayStyle = new("ToolModLocalNameStyle", SystemGlobal);
        [Obsolete] public readonly SetupEntry<int> UpdateAlpha = new("ToolUpdateAlpha", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<bool> UpdateRelease = new("ToolUpdateRelease", SystemGlobal);
        public readonly SetupEntry<bool> UpdateSnapshot = new("ToolUpdateSnapshot", SystemGlobal);
        public readonly SetupEntry<string> FavoriteComps = new("CompFavorites", "[]", SystemGlobal);

        public sealed class DownloadSettings
        {
            public readonly SetupEntry<int> ThreadCount = new("ToolDownloadThread", 63, SystemGlobal);
            public readonly SetupEntry<int> SpeedLimit = new("ToolDownloadSpeed", 42, SystemGlobal);
            public readonly SetupEntry<int> FileSourceResolution = new("ToolDownloadSource", 1, SystemGlobal);
            public readonly SetupEntry<int> VersionSourceResolution = new("ToolDownloadVersion", 1, SystemGlobal);
            public readonly SetupEntry<int> ModSourceResolution = new("ToolDownloadMod", 1, SystemGlobal);
            public readonly SetupEntry<int> CompNameFormatV1 = new("ToolDownloadTranslate", SystemGlobal);
            public readonly SetupEntry<int> CompNameFormatV2 = new("ToolDownloadTranslateV2", 1, SystemGlobal);
            public readonly SetupEntry<bool> IgnoreQuilt = new("ToolDownloadIgnoreQuilt", true, SystemGlobal);
            public readonly SetupEntry<bool> ListenClipboard = new("ToolDownloadClipboard", SystemGlobal);
            public readonly SetupEntry<bool> CheckCertificate = new("ToolDownloadCert", true, SystemGlobal);
            public readonly SetupEntry<bool> AutoSelectInstalledVersion = new("ToolDownloadAutoSelectVersion", true, SystemGlobal);
        }
    }

    public sealed class LaunchSettings
    {
        private const string AdvanceJvmDefault =
            "-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow " +
            "-Djdk.lang.Process.allowAmbiguousCommands=true -Dfml.ignoreInvalidMinecraftCertificates=True " +
            "-Dfml.ignorePatchDiscrepancies=True -Dlog4j2.formatMsgNoLookups=true";

        public readonly SetupEntry<string> ImportedFolders = new("LaunchFolders", SystemGlobal);
        public readonly SetupEntry<int> LauncherVisibility = new("LaunchArgumentVisible", 5, SystemGlobal);
        public readonly SetupEntry<int> ProcessPriority = new("LaunchArgumentPriority", 1, SystemGlobal);
        public readonly SetupEntry<bool> UseHighPerfGraphicCard = new("LaunchAdvanceGraphicCard", true, SystemGlobal);
        public readonly SetupEntry<int> PreferredIpStack = new("LaunchPreferredIpStack", SystemGlobal);
        public readonly SetupEntry<string> Uuid = new("LaunchUuid", SystemGlobal);

        public readonly SetupEntry<string> SelectedVersion = new("LaunchVersionSelect", PathLocal);
        public readonly SetupEntry<string> SelectedFolder = new("LaunchFolderSelect", PathLocal);
        public readonly SetupEntry<int> WindowWidth = new("LaunchArgumentWindowWidth", 854, PathLocal);
        public readonly SetupEntry<int> WindowHeight = new("LaunchArgumentWindowHeight", 480, PathLocal);
        public readonly SetupEntry<int> WindowType = new("LaunchArgumentWindowType", 1, PathLocal);

        // 游戏实例设置中也有以下项

        public readonly SetupEntry<string> SelectedJava = new("LaunchArgumentJavaSelect", SystemGlobal);
        public readonly SetupEntry<bool> OptimizeMemory = new("LaunchArgumentRam", SystemGlobal);

        public readonly SetupEntry<int> IndieResolutionV1 = new("LaunchArgumentIndie", PathLocal);
        public readonly SetupEntry<int> IndieResolutionV2 = new("LaunchArgumentIndieV2", 4, PathLocal);
        public readonly SetupEntry<string> CustomJvmArgs = new("LaunchAdvanceJvm", AdvanceJvmDefault, PathLocal);
        public readonly SetupEntry<string> CustomGameArgs = new("LaunchAdvanceGame", PathLocal);
        public readonly SetupEntry<bool> DisableJlw = new("LaunchAdvanceDisableJLW", PathLocal);
        public readonly SetupEntry<bool> DisableRw = new("LaunchAdvanceDisableRW", PathLocal);
        public readonly SetupEntry<string> PreLaunchCommand = new("LaunchAdvanceRun", PathLocal);
        public readonly SetupEntry<bool> PreLaunchCommandWait = new("LaunchAdvanceRunWait", true, PathLocal);
        public readonly SetupEntry<int> MemoryAllocationResolution = new("LaunchRamType", PathLocal);
        public readonly SetupEntry<int> CustomMemorySize = new("LaunchRamCustom", 15, PathLocal);
        public readonly SetupEntry<string> WindowTitle = new("LaunchArgumentTitle", PathLocal);
        public readonly SetupEntry<string> VersionExtraInfo = new("LaunchArgumentInfo", "PCL", PathLocal);
    }

    public sealed class McInstanceSettings
    {
        public readonly SetupEntry<int> DisableAssetsVerificationV1 = new("VersionAdvanceAssets", MinecraftInstance);
        public readonly SetupEntry<bool> DisableAssetsVerificationV2 = new("VersionAdvanceAssetsV2", MinecraftInstance);
        public readonly SetupEntry<bool> DisableJavaVerification = new("VersionAdvanceJava", MinecraftInstance);
        public readonly SetupEntry<bool> UseProxy = new("VersionAdvanceUseProxyV2", MinecraftInstance);
        public readonly SetupEntry<string> ServerAddress = new("VersionServerEnter", MinecraftInstance);
        public readonly SetupEntry<int> AuthRequirementType = new("VersionServerLoginRequire", MinecraftInstance);
        public readonly SetupEntry<string> AuthServerAddress = new("VersionServerAuthServer", MinecraftInstance);
        public readonly SetupEntry<string> AuthRegisterUrl = new("VersionServerAuthRegister", MinecraftInstance);
        public readonly SetupEntry<string> AuthServerDisplayName = new("VersionServerAuthName", MinecraftInstance);
        public readonly SetupEntry<bool> AuthLocked = new("VersionServerLoginLock", MinecraftInstance);

        // 启动设置中也有以下项

        public readonly SetupEntry<string> SelectedJava = new("VersionArgumentJavaSelect", "使用全局设置", MinecraftInstance);
        public readonly SetupEntry<int> OptimizeMemory = new("VersionRamOptimize", MinecraftInstance);
        public readonly SetupEntry<int> IndieV1 = new("VersionArgumentIndie", -1, MinecraftInstance);
        public readonly SetupEntry<bool> IndieV2 = new("VersionArgumentIndieV2", MinecraftInstance);
        public readonly SetupEntry<string> CustomJvmArgs = new("VersionAdvanceJvm", MinecraftInstance);
        public readonly SetupEntry<string> CustomGameArgs = new("VersionAdvanceGame", MinecraftInstance);
        [Obsolete] public readonly SetupEntry<bool> DisableJlwObsolete = new("VersionAdvanceDisableJlw", MinecraftInstance);
        public readonly SetupEntry<bool> DisableJlw = new("VersionAdvanceDisableJLW", MinecraftInstance);
        public readonly SetupEntry<bool> DisableRw = new("VersionAdvanceDisableRW", MinecraftInstance);
        public readonly SetupEntry<string> PreLaunchCommand = new("VersionAdvanceRun", MinecraftInstance);
        public readonly SetupEntry<bool> PreLaunchCommandWait = new("VersionAdvanceRunWait", true, MinecraftInstance);
        public readonly SetupEntry<int> MemoryAllocationResolution = new("VersionRamType", 2, MinecraftInstance);
        public readonly SetupEntry<int> CustomMemorySize = new("VersionRamCustom", 15, MinecraftInstance);
        public readonly SetupEntry<string> WindowTitle = new("VersionArgumentTitle", MinecraftInstance);
        public readonly SetupEntry<bool> UseNonGlobalWindowTitle = new("VersionArgumentTitleEmpty", MinecraftInstance);
        public readonly SetupEntry<string> VersionExtraInfo = new("VersionArgumentInfo", MinecraftInstance);
    }
}