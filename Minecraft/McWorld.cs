using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using fNbt;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft;

/// <summary>
/// 存档。
/// </summary>
public class McWorld {
    /// <summary>
    /// 存档路径。文件夹，以 “\” 结尾。
    /// </summary>
    public string SavePath { get; }

    /// <summary>
    /// 版本名。
    /// </summary>
    public string? VersionName { get; private set; }

    /// <summary>
    /// 版本 ID。
    /// </summary>
    public int? VersionId { get; private set; }

    /// <summary>
    /// 存档。
    /// </summary>
    /// <param name="savePath">存档路径。文件夹，以 “\” 结尾。</param>
    public McWorld(string savePath) {
        SavePath = savePath.EndsWith('\\') ? savePath : savePath + "\\";
    }

    /// <summary>
    /// 获取level.dat或level.dat_old文件的路径。
    /// </summary>
    public string LevelDatPath =>
        File.Exists(SavePath + "level.dat") ? SavePath + "level.dat" : SavePath + "level.dat_old";

    /// <summary>
    /// 读取存档。返回是否成功。
    /// </summary>
    public async Task<bool> ReadAsync(CancellationToken cancelToken = default) {
        try {
            var gameVersion = await NbtFileHandler.ReadNbtFileAsync<NbtCompound>(LevelDatPath, "Version", cancelToken);
            
            if (gameVersion == null) {
                LogWrapper.Warn("World", "Version 标签存在问题，读取失败");
                return false;
            }

            VersionName = gameVersion.Get<NbtString>("Name")?.Value;
            VersionId = gameVersion.Get<NbtInt>("Id")?.Value;

            if (!string.IsNullOrEmpty(VersionName) && VersionId != null) {
                return true;
            } 
            LogWrapper.Warn("World", "版本名称或ID无效，读取失败");
            return false;
        } catch (OperationCanceledException) {
            LogWrapper.Info("World", $"读取操作已取消: {SavePath}");
            return false;
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "World", "读取存档时出错");
            return false;
        }
    }
}
