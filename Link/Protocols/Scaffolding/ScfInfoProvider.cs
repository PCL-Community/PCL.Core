using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace PCL.Core.Link.Protocols.Scaffolding;

public static class ScfInfoProvider
{
    public enum PlayerKind
    {
        Host,
        Guest
    }
    public class ScfPlayerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public required PlayerKind Kind { get; init; }
        public JsonObject ToJsonObject()
        {
            return new JsonObject
            {
                ["name"] = Name,
                ["machine_id"] = MachineId,
                ["vendor"] = Vendor,
                ["kind"] = Kind == PlayerKind.Host ? "HOST" : "GUEST"
            };
        }
        public static ScfPlayerInfo FromJsonObject(JsonObject? obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            // 检查必要的字段是否存在
            if (obj["name"] == null || obj["machine_id"] == null || obj["vendor"] == null || obj["kind"] == null)
            {
                throw new ArgumentException("Missing required fields in JSON object");
            }

            var kindStr = obj["kind"]!.ToString();
            var kind = kindStr switch
            {
                "HOST" => PlayerKind.Host,
                "GUEST" => PlayerKind.Guest,
                _ => throw new ArgumentException($"Invalid player kind value: {kindStr}")
            };

            return new ScfPlayerInfo
            {
                Name = obj["name"]!.ToString(),
                MachineId = obj["machine_id"]!.ToString(),
                Vendor = obj["vendor"]!.ToString(),
                Kind = kind
            };
        }
    }

    public static List<ScfPlayerInfo> PlayerList { get; set; } = [];
}