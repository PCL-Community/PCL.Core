using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using PCL.Core.Model;
using System.Collections.Concurrent;

namespace PCL.Core.Helper;

public class JavaManage
{
    private List<Java> _javas = [];
    public List<Java> JavaList => [.. _javas];

    private void SortJavaList()
    {
        _javas = (from j in _javas
            orderby j.Version descending, j.Brand
            select j).ToList();
    }

    private Task? _scanTask = null;
    /// <summary>
    /// 扫描 Java 会对当前已有的结果进行选择性保留
    /// </summary>
    /// <returns></returns>
    public async Task ScanJava()
    {
        if (_scanTask == null || _scanTask.IsCompleted)
            _scanTask = Task.Run(async () =>
            {
                var javaPaths = new ConcurrentBag<string>();

                Task[] searchTasks = [
                    Task.Run(() => ScanRegistryForJava(ref javaPaths)),
                    Task.Run(() => ScanDefaultInstallPaths(ref javaPaths)),
                    Task.Run(() => ScanPathEnvironmentVariable(ref javaPaths)),
                    Task.Run(() => ScanMicrosoftStoreJava(ref javaPaths))
                    ];
                await Task.WhenAll(searchTasks);

                // 记录之前设置为禁用的 Java
                var disabledJava = from j in _javas where !j.IsEnabled select j.JavaExePath;
                // 新搜索到的 Java 路径
                var newJavaList = new HashSet<string>(
                    _javas
                        .Select(x => x.JavaExePath)
                        .Concat(javaPaths)
                        .Select(x => x.TrimEnd(@"\".ToCharArray())),
                    StringComparer.OrdinalIgnoreCase);

                var ret = newJavaList
                    .Where(x => !x.Contains("javapath"))
                    .Select(x => Java.Parse(x))
                    .Where(x => x != null)
                    .ToList();
                foreach (var j in ret)
                {
                    if (disabledJava.Contains(j.JavaExePath))
                        j.IsEnabled = false;
                }

                _javas = ret;
                SortJavaList();
            });
        await _scanTask;
    }

    public void Add(Java j)
    {
        if (j == null)
            throw new ArgumentNullException(nameof(j));
        if (HasJava(j.JavaExePath))
            return;
        _javas.Add(j);
        SortJavaList();
    }

    public void Add(string javaExe)
    {
        if (javaExe == null)
            throw new ArgumentNullException(nameof(javaExe));
        if (HasJava(javaExe))
            return;
        var temp = Java.Parse(javaExe);
        if (temp == null)
            return;
        _javas.Add(temp);
        SortJavaList();
    }

    public bool HasJava(string javaExe)
    {
        if (javaExe == null)
            throw new ArgumentNullException(nameof(javaExe));
        if (!File.Exists(javaExe))
            throw new ArgumentException("Not a valid java file");
        return _javas.Any(x => x.JavaExePath == javaExe);
    }

    public async Task<List<Java>> SelectSuitableJava(Version minVerison, Version maxVersion)
    {
        if (_javas.Count == 0)
            await ScanJava();
        return (from j in _javas
            where j.IsStillAvailable && j.IsEnabled && j.Version >= minVerison && j.Version <= maxVersion
            orderby j.Version, j.IsJre, j.Brand
            select j).ToList();
    }

    /// <summary>
    /// 检查并移除已不存在的 Java
    /// </summary>
    /// <returns></returns>
    public void CheckJavaAvailability()
    {
        _javas = [..from j in _javas where j.IsStillAvailable select j];
    }

    private static void ScanRegistryForJava(ref ConcurrentBag<string> javaPaths)
    {
        // JavaSoft
        var registryPaths = new List<string>
        {
            @"SOFTWARE\JavaSoft\Java Development Kit",
            @"SOFTWARE\JavaSoft\Java Runtime Environment",
            @"SOFTWARE\WOW6432Node\JavaSoft\Java Development Kit",
            @"SOFTWARE\WOW6432Node\JavaSoft\Java Runtime Environment"
        };

        foreach (var regPath in registryPaths)
        {
            using var regKey = Registry.LocalMachine.OpenSubKey(regPath);
            if (regKey == null) continue;
            foreach (var subKeyName in regKey.GetSubKeyNames())
            {
                using var subKey = regKey.OpenSubKey(subKeyName);
                var javaHome = subKey?.GetValue("JavaHome") as string;
                if (string.IsNullOrEmpty(javaHome)) continue;
                var javaExePath = Path.Combine(javaHome, "bin", "java.exe");
                if (File.Exists(javaExePath)) javaPaths.Add(javaExePath);
            }
        }

        //Brand Java Register Path
        string[] brandKeyNames = [
            @"SOFTWARE\Azul Systems\Zulu",
            @"SOFTWARE\BellSoft\Liberica"
            ];
        foreach (var key in brandKeyNames)
        {
            var zuluKey = Registry.LocalMachine.OpenSubKey(key);
            if (zuluKey != null)
            {
                foreach (var subKeyName in zuluKey.GetSubKeyNames())
                {
                    var path = zuluKey.OpenSubKey(subKeyName)?.GetValue("InstallationPath") as string;
                    if (path == null) continue;
                    var javaExePath = Path.Combine(path, "bin", "java.exe");
                    if (!File.Exists(javaExePath)) continue;
                    javaPaths.Add(javaExePath);
                }
            }
        }
    }

    // 可能的目录关键词列表
    private static readonly string[] mostPossibleKeyWords =
    [
        "java", "jdk", "jre",
        "dragonwell", "azul", "zulu", "oracle", "open", "amazon", "corretto", "eclipse" , "temurin", "hotspot", "semeru", "kona", "bellsoft"
    ];
    
    private static readonly string[] possibleKeyWords =
    [
        "environment", "env", "runtime", "x86_64", "amd64", "arm64",
        "pcl", "hmcl", "baka", "minecraft", "microsoft"
    ];

    private static readonly string[] totalKeyWords = [..mostPossibleKeyWords.Concat(possibleKeyWords)];

    // 最大文件夹搜索深度
    const int MAX_SEARCH_DEPTH = 12;

    private static void ScanDefaultInstallPaths(ref ConcurrentBag<string> javaPaths)
    {
        // 准备欲搜索目录
        var programFilesPaths = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 特定目录搜索
            string[] keyFolders =
            [
                "Program Files",
                "Program Files (x86)",
                "Programs"
            ];
            var isDriverSuitable = (DriveInfo d) => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable);
            programFilesPaths.AddRange(
                from driver in DriveInfo.GetDrives()
                where isDriverSuitable(driver)
                from keyFolder in keyFolders
                select Path.Combine(driver.Name, keyFolder));
            // 根目录搜索
            foreach (var dri in from d in DriveInfo.GetDrives() where isDriverSuitable(d) select d.Name)
            {
                try{
                    programFilesPaths.AddRange(from dir in Directory.EnumerateDirectories(dri)
                                            where mostPossibleKeyWords.Any(x => dir.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)
                                            select dir);
                }catch(UnauthorizedAccessException){
                    continue;
                }
            }
        }
        else
        {
            programFilesPaths.AddRange(new List<string> {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            });
        }
        programFilesPaths = [.. programFilesPaths.Where(x => !string.IsNullOrEmpty(x) && Directory.Exists(x)).Distinct()];

        // 使用 广度优先搜索 查找 Java 文件
        foreach (var rootPath in programFilesPaths)
        {
            var queue = new Queue<(string path, int depth)>();
            queue.Enqueue((rootPath, 0));
            while (queue.Count > 0)
            {
                var (currentPath, depth) = queue.Dequeue();
                if (depth > MAX_SEARCH_DEPTH) continue;
                try
                {
                    // 只遍历包含关键字的目录
                    var subDirs = Directory.EnumerateDirectories(currentPath)
                        .Where(x => totalKeyWords.Any(k => x.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
                    foreach (var dir in subDirs)
                    {
                        // 准备可能的 Java 路径
                        List<string> potentialJavas = [
                            Path.Combine(dir, "java.exe")
                            ];
                        potentialJavas = [.. potentialJavas.Where(File.Exists)];
                        
                        // 存在 Java，节点达到目标
                        if (potentialJavas.Any())
                            foreach (var javaPath in potentialJavas) javaPaths.Add(javaPath);
                        else
                            queue.Enqueue((dir, depth + 1));
                    }
                }
                catch { /* 忽略无权限等异常 */ }
            }
        }
    }

    private static void ScanPathEnvironmentVariable(ref ConcurrentBag<string> javaPaths)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return;

        var paths = pathEnv.Split([';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var targetPath in paths)
        {
            var javaExePath = Path.Combine(targetPath, "java.exe");
            if (File.Exists(javaExePath))
            {
                javaPaths.Add(javaExePath);
            }
        }
    }

    private static void ScanMicrosoftStoreJava(ref ConcurrentBag<string> javaPaths)
    {
        var storeJavaFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Packages",
            "Microsoft.4297127D64EC6_8wekyb3d8bbwe", // Ms Java 的固定下载地址
            "LocalCache",
            "Local",
            "runtime");
        if (!Directory.Exists(storeJavaFolder))
            return;
        // 搜索第一级目录：以"java-runtime"开头的文件夹
        foreach (var runtimeDir in Directory.EnumerateDirectories(storeJavaFolder))
        {
            var dirName = Path.GetFileName(runtimeDir);
            if (!dirName.StartsWith("java-runtime"))
                continue;

            // 搜索第二级目录：平台架构目录 (如 windows-x64)
            foreach (var archDir in Directory.EnumerateDirectories(runtimeDir))
            {
                // 搜索第三级目录：具体运行时版本目录
                foreach (var versionDir in Directory.EnumerateDirectories(archDir))
                {
                    // 检查bin/java.exe是否存在
                    var javaExePath = Path.Combine(versionDir, "bin", "java.exe");
                    if (File.Exists(javaExePath))
                    {
                        javaPaths.Add(javaExePath);
                    }
                }
            }
        }
    }

    public List<JavaLocalCache> GetCache()
    {
        return (from j in _javas
            select new JavaLocalCache
            {
                Path = j.JavaExePath,
                IsEnable = j.IsEnabled
            }).ToList();
    }

    public void SetCache(List<JavaLocalCache> caches)
    {
        foreach (var cache in caches)
        {
            try
            {
                var targetInRecord = _javas.First(x => x.JavaExePath == cache.Path);
                targetInRecord.IsEnabled = cache.IsEnable;
            }
            catch
            {
                var temp = Java.Parse(cache.Path);
                if (temp == null)
                    continue;
                temp.IsEnabled = cache.IsEnable;
                _javas.Add(temp);
            }
        }
    }
}

[Serializable]
public class JavaLocalCache
{
    public string Path { get; set; } = "";
    public bool IsEnable { get; set; }
}
