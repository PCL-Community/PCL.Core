using PCL.Core.Utils.Secret;

namespace PCL.Core.ProgramSetup.SourceManage;

public sealed class CombinedMigrationSetupSourceManager(ISetupSourceManager oldSource, ISetupSourceManager newSource)
    : ISetupSourceManager
{
    private static readonly object _GlobalLock = new();
    public required bool ProcessValueAsEncrypted { get; init; }
    
    public string? Get(string key, string? gamePath = null)
    {
        lock (_GlobalLock)
        {
            var result = newSource.Get(key, gamePath);
            var oldValue = oldSource.Remove(key, gamePath);
            if (result is null && oldValue is not null)
            {
                newSource.Set(
                    key,
                    ProcessValueAsEncrypted
                        ? EncryptHelper.SecretEncrypt(EncryptHelper.SecretDecryptOld(oldValue))
                        : oldValue,
                    gamePath);
                result = oldValue;
            }
            return result;
        }
    }

    public string? Set(string key, string value, string? gamePath = null)
    {
        lock (_GlobalLock)
        {
            var result = newSource.Set(key, value, gamePath);
            var oldValue = oldSource.Remove(key, gamePath);
            result ??= oldValue;
            return result;
        }
    }

    public string? Remove(string key, string? gamePath = null)
    {
        lock (_GlobalLock)
        {
            var result = newSource.Remove(key, gamePath);
            var oldValue = oldSource.Remove(key, gamePath);
            result ??= oldValue;
            return result;
        }
    }
}