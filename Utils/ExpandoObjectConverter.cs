// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// 不知道 o4-mini 到底从哪找来的这段源码，据它所说是 .NET 8 标准库里面的
// 我找了半天没找到来源，但是这个 Converter 确实是能用的
// .NET 标准库的源码以 MIT 许可证开源，不论如何先把它的许可丢在这里
// 注：这段源码有一小部分内容是经过修改的

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCL.Core.Utils;

// ReSharper disable All
#nullable disable

public sealed class ExpandoObjectConverter : JsonConverter<ExpandoObject>
{
    public ExpandoObjectConverter() { }
    
    public static readonly ExpandoObjectConverter Default = new ExpandoObjectConverter();

    public override ExpandoObject Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
            JsonElement root = document.RootElement;
            return Read(root, options);
        }
    }

    public static ExpandoObject Read(
        JsonElement element,
        JsonSerializerOptions options)
    {
        ExpandoObject expandoObject = new ExpandoObject();
        IDictionary<string, object> dict = expandoObject;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            object value = JsonSerializer.Deserialize<object>(
                property.Value.GetRawText(), options);
            dict.Add(property.Name, value);
        }
        return expandoObject;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ExpandoObject value,
        JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        foreach (KeyValuePair<string, object> kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}
