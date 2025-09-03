using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PCL.Core.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace PCL.Core.App.Configuration.Impl;

/// <summary>
/// 提供 YAML 格式的键值文件读写。当提供的文件找不到时，将自动读取同名 JSON 文件并将其转换到 YAML。
/// </summary>
public class YamlFileProvider : CommonFileProvider, IEnumerableKeyProvider
{
    private readonly YamlMappingNode _rootNode;

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithEnforceRequiredMembers()
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithQuotingNecessaryStrings(true)
        .Build();

    private static YamlMappingNode? _LoadFile(string path)
    {
        if (!File.Exists(path)) return null;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        try
        {
            var yaml = new YamlStream();
            yaml.Load(reader);
            if (yaml.Documents.Count == 0) return [];
            var rootNode = yaml.Documents[0].RootNode;
            return rootNode as YamlMappingNode ?? throw new FileInitException(path, $"Invalid root node type: {rootNode.NodeType}");
        }
        catch (Exception ex)
        {
            if (ex is FileInitException) throw;
            throw new FileInitException(path, "Failed to load YAML content", ex);
        }
    }

    public YamlFileProvider(string path) : base(path)
    {
        var rootNode = _LoadFile(path);
        if (rootNode != null)
        {
            _rootNode = rootNode;
            return;
        }
        var jsonPath = Path.Combine(Basics.GetParentPath(path)!, Path.GetFileNameWithoutExtension(path) + ".json");
        if (File.Exists(jsonPath))
        {
            using var jsonStream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var yamlStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            JsonToYamlConverter.Convert(jsonStream, yamlStream); // yamlStream 会被自动关闭
            _rootNode = _LoadFile(path)!;
            return;
        }
        _rootNode = [];
    }

    public override T Get<T>(string key)
    {
        var result = _rootNode.Children[key];
        return _deserializer.Deserialize<T>(result.ConvertToEventStream().GetParser());
    }

    public override void Set<T>(string key, T value)
    {
        var emitter = new YamlNodeEmitter();
        _serializer.Serialize(emitter, value);
        _rootNode.Children[key] = emitter.SingleRootNode;
    }

    public override bool Exists(string key)
    {
        return _rootNode.Children.ContainsKey(key);
    }

    public override void Remove(string key)
    {
        _rootNode.Children.Remove(key);
    }

    public override void Sync()
    {
        if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".bak", true);
        using var stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        _serializer.Serialize(writer, _rootNode);
    }

    public IEnumerable<string> Keys => _rootNode.Select(pair => pair.Key.ToString());
}
