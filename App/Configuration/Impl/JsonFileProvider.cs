using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using PCL.Core.IO;

namespace PCL.Core.App.Configuration.Impl;

public class JsonFileProvider : IKeyValueFileProvider
{
    public string FilePath { get; }

    private readonly JsonObject _rootElement;

    private static readonly JsonDocumentOptions _DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static readonly JsonSerializerOptions _SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public JsonFileProvider(string path)
    {
        FilePath = path;
        JsonNode? parseResult;
        try
        {
            var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            parseResult = JsonNode.Parse(stream, documentOptions: _DocumentOptions);
        }
        catch (Exception ex) { throw new FileInitException(path, "Failed to read JSON file", ex); }
        if (parseResult is not JsonObject rootElement) throw new FileInitException(path,
            $"Invalid root element type: {parseResult?.GetValueKind().ToString() ?? "Empty"}");
        _rootElement = rootElement;
    }

    public T Get<T>(string key)
    {
        var result = _rootElement[key];
        return result == null
            ? throw new KeyNotFoundException($"Not found: '{key}'")
            : result.Deserialize<T>()!;
    }

    public void Set<T>(string key, T value)
    {
        _rootElement[key] = JsonSerializer.SerializeToNode(value);
    }

    public bool Exists(string key)
    {
        return _rootElement.ContainsKey(key);
    }

    public void Remove(string key)
    {
        _rootElement.Remove(key);
    }

    public void Sync()
    {
        var stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var writer = new Utf8JsonWriter(stream);
        _rootElement.WriteTo(writer, _SerializerOptions);
    }
}
