

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.Utils.Mod;

namespace PCL.Core.Utils.Modpack;


public class CurseForge
{
    public string? Author;
    public string? Name;
    public string? Version;
    public string? InputFolder;
    public string? OutputFile;
    public List<CfModloaders?> Modloaders = new();
    private List<CfModFile> ModFiles = new();
    private JsonNode? Manifest;
    private List<string> AddToFile = new();
    private dynamic? Entry;
    private void OnFinsh()
    {
        Task.Run(() =>
        {
            this.Export();
        });
    }

    private void Start()
    {
        List<Task?> tasks = [
            this.PrepareMods(),
            this.PrepareOverride()
        ];
        Task.WaitAll(tasks.ToArray());
        this.PrepareJson().GetAwaiter().GetResult();
        this.OnFinsh();
    }

    private void Export()
    {
        if(this.OutputFile is null) throw new ArgumentNullException("参数无效：OutputFile");
        using (FileStream fs = new(this.OutputFile, FileMode.Create))
        {
            using (ZipArchive Zipfile = new(fs))
            {
                using(var Entry = Zipfile.CreateEntry("manifest.json").Open())
                {
                    using (MemoryStream memoryStream = new(this.Manifest!.ToJsonString().GetBytes()))
                    {
                        memoryStream.CopyTo(Entry);
                    }
                }
                
                
                
            }
        }
    }

    public async Task<bool> PrepareOverride()
    {
        foreach (var (file,Entry) in this.ModFiles.Zip(this.Entry, (f,e) => (f,e)))
        {
            if(file.FileId == Entry.FileId) continue;
        }
        foreach (var file in Directory.GetFiles(this.InputFolder))
        {
            
        }
        return true;
    }
    public async Task<bool> PrepareJson()
    {
        this.Manifest = new JsonObject()
        {
            ["minecraft"] = new JsonObject()
            {
                ["version"] = this.Version,
                ["modloaders"] = JsonSerializer.Serialize(this.Modloaders)
            },
            ["manifestType"] = "minecraftModpack",
            ["overrides"] = "overrides",
            ["manifestVersion"] = 1,
            ["version"] = this.Version,
            ["author"] =  this.Author,
            ["name"] = this.Name,
            ["files"] = JsonSerializer.Serialize(this.ModFiles)
        };
    }
    public async Task<bool> PrepareMods(dynamic? modHashes = null)
    {
        if (modHashes is null) goto ModCompareFinished;
        this.Entry = modHashes;
        modHashes["modloaders"] = JsonSerializer.Serialize(this.Modloaders);
        string modsFolder = InputFolder?.TrimEnd('/') + "/mods";
        if (Directory.Exists(modsFolder))
        {
            List<string> modHash = new();
            JsonNode? result = await CurseForgeMod.GetModInfomationByHash(modHash);
            if (result is null)  goto ModCompareFinished;
            JsonArray? data = result["data"]?["exactMatches"]?.AsObject().AsArray();
            if (data is null) goto ModCompareFinished;
            foreach (JsonNode? modInfo in data)
            {
                try
                {
                    ModFiles.Add(new CfModFile()
                    {
                        ProjectId = int.Parse(data["id"]?.ToString()),
                        FileId = int.Parse(data["file"]?["id"]?.ToString()),
                        Required = true,
                    });
                }
                catch
                {
                    continue;
                }
            }
        }
        ModCompareFinished:
        return true;
    }
}


public class CfPackVersion
{
    public string Version;
    public List<CfModloaders> Modloaders;
}

public class CfModloaders
{
    public string? Id;
    public bool Primary;
}

public class CfModFile
{
    public int ProjectId;
    public int FileId;
    public bool Required;
}