namespace PCL.Core.ProgramSetup.SourceManage;

public interface ISetupSourceManager
{
    /// <summary>
    /// 获取某个键的值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="gamePath">游戏目录路径</param>
    /// <returns>键的值，如果不存在该键则返回 null</returns>
    string? Get(string key, string? gamePath = null);

    /// <summary>
    /// 设置某个键的值，并获取旧值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">要设为的值</param>
    /// <param name="gamePath">游戏目录路径</param>
    /// <returns>键的旧值，之前不存在该键则返回 null</returns>
    string? Set(string key, string value, string? gamePath = null);

    /// <summary>
    /// 删除某个键，并获取值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="gamePath">游戏目录路径</param>
    /// <returns>键的值，不存在该键则返回 null</returns>
    string? Remove(string key, string? gamePath = null);
}