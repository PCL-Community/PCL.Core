using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PCL.Core.ProgramSetup.FileManager;

public interface ISetupFileSerializer
{
    void Deserialize(Stream source, ConcurrentDictionary<string, string> destination);
    void Serialize(ConcurrentDictionary<string, string> source, Stream destination);
}

public sealed class SetupJsonSerializer : ISetupFileSerializer
{
    public static readonly SetupJsonSerializer Instance = new();

    public void Deserialize(Stream source, ConcurrentDictionary<string, string> destination)
    {
        if (source.Length == 0)
            return;
        if (JsonSerializer.Deserialize<Dictionary<string, string>>(source) is { } dict)
            foreach (var pair in dict)
                destination.TryAdd(pair.Key, pair.Value);
    }

    public void Serialize(ConcurrentDictionary<string, string> source, Stream destination)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(source);
        destination.Write(data, 0, data.Length);
    }
}

public sealed class SetupIniSerializer : ISetupFileSerializer
{
    public static readonly SetupIniSerializer Instance = new();

    public void Deserialize(Stream source, ConcurrentDictionary<string, string> destination)
    {
        using var reader = new StreamReader(source);
        while (reader.ReadLine() is { } line)
        {
            var splitPos = line.IndexOf(':');
            if (splitPos == -1)
                continue;
            var key = line[..splitPos];
            var value = line[(splitPos + 1)..];
            destination.TryAdd(key, value);
        }
    }

    public void Serialize(ConcurrentDictionary<string, string> source, Stream destination)
    {
        using var writer = new StreamWriter(destination);
        foreach (var pair in source)
            writer.WriteLine("{0}:{1}", pair.Key, pair.Value);
    }
}