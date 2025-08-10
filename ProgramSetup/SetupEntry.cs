namespace PCL.Core.ProgramSetup;

public sealed record SetupEntry(SetupEntrySource SourceType, string KeyName, object DefaultValue, bool IsEncrypted = false);

public enum SetupEntrySource
{
    PathLocal,
    SystemGlobal,
    GameInstance
}