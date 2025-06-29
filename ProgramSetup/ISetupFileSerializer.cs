using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PCL.Core.ProgramSetup;

public interface ISetupFileSerializer
{
    ConcurrentDictionary<string, string>? Deserialize(Stream stream);
    string Serialize(ConcurrentDictionary<string, string> dictionary);
}

public sealed class SetupJsonSerializer : ISetupFileSerializer
{
    public static readonly SetupJsonSerializer Instance = new();

    public ConcurrentDictionary<string, string>? Deserialize(Stream stream) =>
        JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(stream);

    public string Serialize(ConcurrentDictionary<string, string> dictionary) =>
        JsonSerializer.Serialize(dictionary);
}

public sealed class SetupIniSerializer : ISetupFileSerializer
{
    public static readonly SetupIniSerializer Instance = new();

    public ConcurrentDictionary<string, string> Deserialize(Stream stream)
    {
        var result = new ConcurrentDictionary<string, string>();
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            var splitPos = line.IndexOf(':');
            if (splitPos == -1)
                continue;
            var key = line.Substring(0, splitPos);
            var value = line.Substring(splitPos + 1);
            result.TryAdd(key, value);
        }
        return result;
    }

    public string Serialize(ConcurrentDictionary<string, string> dictionary) =>
        string.Join(Environment.NewLine, dictionary.Select(pair => $"{pair.Key}:{pair.Value}"));
}