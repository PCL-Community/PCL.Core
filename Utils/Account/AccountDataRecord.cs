using System.Text.Json.Nodes;

namespace PCL.Core.Utils.Account;

public record AccountDataRecord<TProviderData>(TProviderData Data, JsonNode RawData);