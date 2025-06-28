using System;
using static PCL.Core.ProgramSetup.SetupEntrySource;

namespace PCL.Core.ProgramSetup;

public sealed class Setup
{
    public const int SetupVersionNum = 1;
    
    public static Setup Current => throw new NotImplementedException();

    public readonly Identifies Identify = new();
    public readonly Counters Counter = new();
    public readonly Hints Hint = new();
    public readonly Caches Cache = new();
    public readonly SystemSettings System = new();
    public readonly LaunchSettings Launch = new();
    public readonly LinkSettings Link = new();
    public readonly LoginSettings Login = new();
    public readonly ToolSettings Tool = new();
    public readonly UiSettings Ui = new();
    public readonly McInstanceSettings Minecraft = new();

    public sealed class Identifies
    {
        public readonly SetupEntry<string> Identify = new("Identify", SystemGlobal);
        public readonly SetupEntry<int> VersionGlobal = new("SystemSetupVersionReg", SetupVersionNum, SystemGlobal);
        public readonly SetupEntry<int> VersionLocal = new("SystemSetupVersionIni", SetupVersionNum, PathLocal);
        [Obsolete] public readonly SetupEntry<int> JavaListVersion = new("CacheJavaListVersion", SystemGlobal);
    }

    public sealed class Counters
    {
        public readonly SetupEntry<int> LauncherBootCount = new("SystemCount", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> LaunchCount = new("SystemLaunchCount", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> LastVersion = new("SystemLastVersionReg", SystemGlobal, isEncrypted: true);
        [Obsolete] public readonly SetupEntry<int> HighestSavedBetaVersion = new("SystemHighestSavedBetaVersionReg", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> HighestBetaVersion = new("SystemHighestBetaVersionReg", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> HighestAlphaVersion = new("SystemHighestAlphaVersionReg", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> ShowedAnnouncement = new("SystemSystemAnnouncement", SystemGlobal);
    }

    public sealed class Hints
    {
        public readonly SetupEntry<bool> DownloadThreadCount = new("HintDownloadThread", SystemGlobal);
        [Obsolete] public readonly SetupEntry<int> Notice = new("HintNotice", SystemGlobal);
        public readonly SetupEntry<int> DownloadComp = new("HintDownload", SystemGlobal);
        public readonly SetupEntry<bool> InstallPageBack = new("HintInstallBack", SystemGlobal);
        public readonly SetupEntry<bool> HideInstance = new("HintHide", SystemGlobal);
        public readonly SetupEntry<bool> ManuallyInstall = new("HintHandInstall", SystemGlobal);
        public readonly SetupEntry<bool> BuyMinecraft = new("HintBuy", SystemGlobal);
        public readonly SetupEntry<int> ClearRubbish = new("HintClearRubbish", SystemGlobal);
        public readonly SetupEntry<bool> UpdateMod = new("HintUpdateMod", SystemGlobal);
        public readonly SetupEntry<bool> Mainpage = new("HintCustomWarn", SystemGlobal);
        public readonly SetupEntry<bool> MainpageCommand = new("HintCustomCommand", SystemGlobal);
        public readonly SetupEntry<bool> MoreAdvancedSetup = new("HintMoreAdvancedSetup", SystemGlobal);
        public readonly SetupEntry<bool> IndieSetup = new("HintIndieSetup", SystemGlobal);
        public readonly SetupEntry<bool> ProfileSelect = new("HintProfileSelect", SystemGlobal);
        public readonly SetupEntry<bool> ExportConfig = new("HintExportConfig", SystemGlobal);
        public readonly SetupEntry<bool> MaxLogLines = new("HintMaxLog", SystemGlobal);
        public readonly SetupEntry<bool> GamePathNonAscii = new("HintDisableGamePathCheckTip", SystemGlobal);
        public readonly SetupEntry<bool> LauncherEula = new("SystemEula", SystemGlobal);
        public readonly SetupEntry<bool> CommunityLauncher = new("UiLauncherCEHint", true, SystemGlobal);
    }

    public sealed class Caches
    {
        public readonly SetupEntry<string> ExportConfig = new("CacheExportConfig", SystemGlobal);
        public readonly SetupEntry<string> MainpageUrl = new("CacheSavedPageUrl", SystemGlobal);
        public readonly SetupEntry<string> MainpageVersion = new("CacheSavedPageVersion", SystemGlobal);
        public readonly SetupEntry<string> Java = new("LaunchArgumentJavaUser", "[]", SystemGlobal);
        public readonly SetupEntry<string> AuthUuid = new("CacheAuthUuid", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> AuthName = new("CacheAuthName", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> AuthUsername = new("CacheAuthUsername", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> AuthPass = new("CacheAuthPass", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> AuthServer = new("CacheAuthServerServer", SystemGlobal, isEncrypted: true);
    }

    public sealed class SystemSettings
    {
        public readonly DebugSettings Debug = new();
        
        public readonly SetupEntry<string> CacheFolder = new("SystemSystemCache", SystemGlobal);
        public readonly SetupEntry<string> DownloadFolder = new("CacheDownloadFolder", SystemGlobal);
        public readonly SetupEntry<string> HttpProxy = new("SystemHttpProxy", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<bool> UseDefaultProxy = new("SystemUseDefaultProxy", true, SystemGlobal);
        public readonly SetupEntry<bool> DisableHardwareAcceleration = new("SystemDisableHardwareAcceleration", SystemGlobal);
        public readonly SetupEntry<object> Telemetry = new("SystemTelemetry", SystemGlobal); // TODO: It's a bool
        public readonly SetupEntry<string> MirrorChyanKey = new("SystemMirrorChyanKey", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<int> RealTimeLogMaxLineCount = new("SystemMaxLog", 13, SystemGlobal);

        public readonly SetupEntry<int> UpdateResolution = new("SystemSystemUpdate", PathLocal);
        public readonly SetupEntry<int> UpdateBranchResolution = new("SystemSystemUpdateBranch", PathLocal);
        public readonly SetupEntry<int> ActivityResolution = new("SystemSystemActivity", PathLocal);

        public sealed class DebugSettings
        {
            public readonly SetupEntry<bool> Enabled = new("SystemDebugMode", SystemGlobal);
            public readonly SetupEntry<int> AnimationSpeed = new("SystemDebugAnim", 9, SystemGlobal);
            public readonly SetupEntry<bool> AddRandomDelay = new("SystemDebugDelay", SystemGlobal);
            public readonly SetupEntry<bool> DoNotCopyExistingFile = new("SystemDebugSkipCopy", SystemGlobal);
        }
    }

    public sealed class LaunchSettings
    {
        private const string AdvanceJvmDefault = "-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow -Djdk.lang.Process.allowAmbiguousCommands=true -Dfml.ignoreInvalidMinecraftCertificates=True -Dfml.ignorePatchDiscrepancies=True -Dlog4j2.formatMsgNoLookups=true";

        public readonly SetupEntry<string> ImportedMcFolders = new("LaunchFolders", SystemGlobal);
        public readonly SetupEntry<string> JavaSelecting = new("LaunchArgumentJavaSelect", SystemGlobal);
        public readonly SetupEntry<int> LauncherVisible = new("LaunchArgumentVisible", 5, SystemGlobal);
        public readonly SetupEntry<int> ProcessPriority = new("LaunchArgumentPriority", 1, SystemGlobal);
        public readonly SetupEntry<bool> OptimizeRam = new("LaunchArgumentRam", SystemGlobal);
        public readonly SetupEntry<bool> GraphicCardPerformance = new("LaunchAdvanceGraphicCard", true, SystemGlobal);

        public readonly SetupEntry<string> VersionSelecting = new("LaunchVersionSelect", PathLocal);
        public readonly SetupEntry<string> FolderSelecting = new("LaunchFolderSelect", PathLocal);
        public readonly SetupEntry<string> VersionTypeExtraInfo = new("LaunchArgumentInfo", "PCL", PathLocal);
        public readonly SetupEntry<int> IndieResolutionV1 = new("LaunchArgumentIndie", PathLocal);
        public readonly SetupEntry<int> IndieResolutionV2 = new("LaunchArgumentIndieV2", 4, PathLocal);
        public readonly SetupEntry<string> WindowTitle = new("LaunchArgumentTitle", PathLocal);
        public readonly SetupEntry<int> WindowWidth = new("LaunchArgumentWindowWidth", 854, PathLocal);
        public readonly SetupEntry<int> WindowHeight = new("LaunchArgumentWindowHeight", 480, PathLocal);
        public readonly SetupEntry<int> WindowType = new("LaunchArgumentWindowType", 1, PathLocal);
        public readonly SetupEntry<string> CustomJvmArgs = new("LaunchAdvanceJvm", AdvanceJvmDefault, PathLocal);
        public readonly SetupEntry<string> CustomGameArgs = new("LaunchAdvanceGame", PathLocal);
        public readonly SetupEntry<string> PreLaunchRun = new("LaunchAdvanceRun", PathLocal);
        public readonly SetupEntry<bool> PreLaunchRunWait = new("LaunchAdvanceRunWait", true, PathLocal);
        public readonly SetupEntry<bool> DisableJlw = new("LaunchAdvanceDisableJLW", PathLocal);
        public readonly SetupEntry<bool> DisableRw = new("LaunchAdvanceDisableRW", PathLocal);
        public readonly SetupEntry<int> RamAllocResolution = new("LaunchRamType", PathLocal);
        public readonly SetupEntry<int> CustomRamSize = new("LaunchRamCustom", 15, PathLocal);
    }

    public sealed class LinkSettings
    {
        public readonly SetupEntry<bool> EulaAgreed = new("LinkEula", SystemGlobal);
        public readonly SetupEntry<string> UserName = new("LinkName", SystemGlobal);
        public readonly SetupEntry<bool> DoFirstTimeNetTest = new("LinkFirstTimeNetTest", true, SystemGlobal);
    }

    public sealed class LoginSettings
    {
        public readonly SetupEntry<string> LegacyName = new("LoginLegacyName", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> MsJson = new("LoginMsJson", "{}", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> MsAuthType = new("LoginMsAuthType", "0", SystemGlobal);
    }

    public sealed class ToolSettings
    {
        public readonly DownloadSettings Download = new();
        
        public readonly SetupEntry<bool> FixAuthLib = new("ToolFixAuthlib", true, SystemGlobal);
        public readonly SetupEntry<bool> SetMcToChinese = new("ToolHelpChinese", true, SystemGlobal);
        public readonly SetupEntry<int> ModLocalNameStyle = new("ToolModLocalNameStyle", SystemGlobal);
        public readonly SetupEntry<int> UpdateAlpha = new("ToolUpdateAlpha", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<bool> UpdateRelease = new("ToolUpdateRelease", SystemGlobal);
        public readonly SetupEntry<bool> UpdateSnapshot = new("ToolUpdateSnapshot", SystemGlobal);
        public readonly SetupEntry<string> UpdateReleaseLast = new("ToolUpdateReleaseLast", SystemGlobal);
        public readonly SetupEntry<string> UpdateSnapshotLast = new("ToolUpdateSnapshotLast", SystemGlobal);
        public readonly SetupEntry<string> CompFavorites = new("CompFavorites", "[]", SystemGlobal);

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
            public readonly SetupEntry<bool> EnableCert = new("ToolDownloadCert", true, SystemGlobal);
            public readonly SetupEntry<bool> AutoSelectOnVersionInstalled = new("ToolDownloadAutoSelectVersion", true, SystemGlobal);
        }
    }

    public sealed class UiSettings
    {
        public readonly UiHideSettings Hide = new();
        public readonly MusicSettings Music = new();
        public readonly BackgroundSettings Background = new();
        public readonly MainpageSettings Mainpage = new();
        
        public readonly SetupEntry<int> DarkMode = new("UiDarkMode", 2, SystemGlobal);
        public readonly SetupEntry<int> AniFpsLimit = new("UiAniFPS", 59, SystemGlobal);
        
        public readonly SetupEntry<int> WindowHeight = new("WindowHeight", 550, PathLocal);
        public readonly SetupEntry<int> WindowWidth = new("WindowWidth", 900, PathLocal);
        public readonly SetupEntry<int> Transparent = new("UiLauncherTransparent", 600, PathLocal);
        public readonly SetupEntry<int> ColorHue = new("UiLauncherHue", 180, PathLocal);
        public readonly SetupEntry<int> ColorSat = new("UiLauncherSat", 80, PathLocal);
        public readonly SetupEntry<int> ColorDelta = new("UiLauncherDelta", 90, PathLocal);
        public readonly SetupEntry<int> ColorLight = new("UiLauncherLight", 20, PathLocal);
        public readonly SetupEntry<int> Theme = new("UiLauncherTheme", PathLocal);
        public readonly SetupEntry<string> ThemeGold = new("UiLauncherThemeGold", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> HiddenThemesV1 = new("UiLauncherThemeHide", "0|1|2|3|4", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<string> HiddenThemesV2 = new("UiLauncherThemeHide2", "0|1|2|3|4", SystemGlobal, isEncrypted: true);
        public readonly SetupEntry<bool> ShowLogoOnBooting = new("UiLauncherLogo", true, PathLocal);
        public readonly SetupEntry<int> LogoResolution = new("UiLogoType", 1, PathLocal);
        public readonly SetupEntry<string> LogoTextCustom = new("UiLogoText", PathLocal);
        public readonly SetupEntry<bool> TitleLeftAlign = new("UiLogoLeft", PathLocal);
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
            public readonly SetupEntry<bool> Stop = new("UiMusicStop", PathLocal);
            public readonly SetupEntry<bool> Start = new("UiMusicStart", PathLocal);
            public readonly SetupEntry<bool> Random = new("UiMusicRandom", true, PathLocal);
            public readonly SetupEntry<bool> Smtc = new("UiMusicSMTC", true, PathLocal);
            public readonly SetupEntry<bool> Auto = new("UiMusicAuto", true, PathLocal);
        }

        public sealed class BackgroundSettings
        {
            public readonly SetupEntry<bool> IsColorful = new("UiBackgroundColorful", true, PathLocal);
            public readonly SetupEntry<int> Opacity = new("UiBackgroundOpacity", 1000, PathLocal);
            public readonly SetupEntry<int> BlurRadius = new("UiBackgroundBlur", PathLocal);
            public readonly SetupEntry<int> SuitResolution = new("UiBackgroundSuit", PathLocal);
            public readonly SetupEntry<bool> AdvanceBlur = new("UiBlur", PathLocal);
            public readonly SetupEntry<int> AdvanceBlurValue = new("UiBlurValue", 16, PathLocal);
        }

        public sealed class MainpageSettings
        {
            public readonly SetupEntry<int> TypeResolution = new("UiCustomType", PathLocal);
            public readonly SetupEntry<int> PresetIndex = new("UiCustomPreset", PathLocal);
            public readonly SetupEntry<string> CustomUrl = new("UiCustomNet", PathLocal);
        }
    }

    public sealed class McInstanceSettings
    {
        public SetupEntry<int> IndieV1 = new("VersionArgumentIndie", -1, MinecraftInstance);
        public SetupEntry<bool> IndieV2 = new("VersionArgumentIndieV2", MinecraftInstance);
        public SetupEntry<string> CustomJvmArgs = new("VersionAdvanceJvm", MinecraftInstance);
        public SetupEntry<string> CustomGameArgs = new("VersionAdvanceGame", MinecraftInstance);
        public SetupEntry<int> DisableAssetsVerificationV1 = new("VersionAdvanceAssets", MinecraftInstance);
        public SetupEntry<bool> DisableAssetsVerificationV2 = new("VersionAdvanceAssetsV2", MinecraftInstance);
        public SetupEntry<bool> DisableJavaVerification = new("VersionAdvanceJava", MinecraftInstance);
        public SetupEntry<bool> DisableJlwObsolete = new("VersionAdvanceDisableJlw", MinecraftInstance);
        public SetupEntry<bool> DisableJlw = new("VersionAdvanceDisableJLW", MinecraftInstance);
        public SetupEntry<bool> DisableRw = new("VersionAdvanceDisableRW", MinecraftInstance);
        public SetupEntry<string> PreLaunchRun = new("VersionAdvanceRun", MinecraftInstance);
        public SetupEntry<bool> PreLaunchRunWait = new("VersionAdvanceRunWait", true, MinecraftInstance);
        public SetupEntry<bool> UseProxyV2 = new("VersionAdvanceUseProxyV2", MinecraftInstance);
        public SetupEntry<int> RamAllocResolution = new("VersionRamType", 2, MinecraftInstance);
        public SetupEntry<int> CustomRamSize = new("VersionRamCustom", 15, MinecraftInstance);
        public SetupEntry<int> OptimizeRam = new("VersionRamOptimize", MinecraftInstance);
        public SetupEntry<string> Title = new("VersionArgumentTitle", MinecraftInstance);
        public SetupEntry<bool> IsTitleUnset = new("VersionArgumentTitleEmpty", MinecraftInstance);
        public SetupEntry<string> VersionTypeExtraInfo = new("VersionArgumentInfo", MinecraftInstance);
        public SetupEntry<string> JavaSelecting = new("VersionArgumentJavaSelect", "使用全局设置", MinecraftInstance);
        public SetupEntry<string> ServerToEnter = new("VersionServerEnter", MinecraftInstance);
        public SetupEntry<int> ServerLoginRequire = new("VersionServerLoginRequire", MinecraftInstance);
        public SetupEntry<string> ServerAuthRegister = new("VersionServerAuthRegister", MinecraftInstance);
        public SetupEntry<string> ServerAuthName = new("VersionServerAuthName", MinecraftInstance);
        public SetupEntry<string> ServerAuthServer = new("VersionServerAuthServer", MinecraftInstance);
        public SetupEntry<bool> ServerLoginLocked = new("VersionServerLoginLock", MinecraftInstance);
    }
}