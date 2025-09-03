using System.IO;
using System.Text;
using PCL.Core.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace PCL.Core.App.Configuration.Impl;

public class YamlFileProvider : CommonFileProvider
{
    private readonly YamlMappingNode _rootNode;

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithEnforceRequiredMembers()
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithQuotingNecessaryStrings(true)
        .Build();

    public YamlFileProvider(string path) : base(path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var yaml = new YamlStream();
        yaml.Load(reader);
        var rootNode = yaml.Documents[0].RootNode;
        if (rootNode is not YamlMappingNode mappingNode)
            throw new FileInitException(path, $"Invalid root node type: {rootNode.NodeType}");
        _rootNode = mappingNode;
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
        File.Copy(FilePath, FilePath + ".bak", true);
        using var stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        _serializer.Serialize(writer, _rootNode);
    }
}
