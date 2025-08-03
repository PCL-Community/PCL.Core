using System.Text.Json.Nodes;

namespace PCL.Core.Utils.Account;

public record AccountDataProvider<TProviderData>(AccountType Type, TProviderData Data, JsonNode RawData);