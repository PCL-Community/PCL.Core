using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace PCL.Core.Link.Protocols.Scaffolding;

public static class ScfInfoProvider
{
    public class ScfPlayerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public required bool IsHost { get; init; }
        public JsonObject ToJsonObject(bool hasKind = true)
        {
            return new JsonObject
            {
                ["name"] = Name,
                ["machine_id"] = MachineId,
                ["vendor"] = Vendor,
                ["kind"] = hasKind ? IsHost ? "HOST" : "GUEST" : null
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

            var kindStr = obj["kind"]!.GetValue<string>();
            var kind = kindStr switch
            {
                "HOST" => true,
                "GUEST" => false,
                _ => throw new ArgumentException($"Invalid player kind value: {kindStr}")
            };

            return new ScfPlayerInfo
            {
                Name = obj["name"]!.GetValue<string>(),
                MachineId = obj["machine_id"]!.GetValue<string>(),
                Vendor = obj["vendor"]!.GetValue<string>(),
                IsHost = kind
            };
        }
    }

    /// <summary>
    /// 键值：机器 ID,
    /// 值：玩家信息
    /// </summary>
    public static ConcurrentDictionary<string, ScfPlayerInfo> PlayerDict { get; set; } = [];
    public static int? ServerPort { get; set; } = null;
}