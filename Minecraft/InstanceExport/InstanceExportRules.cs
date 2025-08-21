using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.InstanceExport
{
    public record InstanceExportRules
    {
        public List<string> DisabledFiles { get; set; }
        public List<RuleNode> Rules { get; set; }
    }

    public record RuleNode
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsCheckedByDefault { get; set; }
        public List<string> ExportRules { get; set; }
        public List<RequireRule> Requires { get; set; }
        public List<RuleNode> Children { get; set; }

        [JsonPropertyName("$ENUM_FILES_DEPTH")]
        public int? EnumFilesDepth { get; set; }

        [JsonPropertyName("$ENUM_DIRECTORIES_DEPTH")]
        public int? EnumDirectoriesDepth { get; set; }

        [JsonPropertyName("$ENUM_FILES")]
        public List<string> EnumFiles { get; set; }

        [JsonPropertyName("$ENUM_DIRECTORIES")]
        public List<string> EnumDirectories { get; set; }
    }

    public record RequireRule
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RequireOperator Operator { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public List<RequireValue> Values { get; set; }
    }

    public enum RequireOperator
    {
        None,
        And,
        Or
    }

    public enum RequireValue
    {
        ModLoader,
        Optifine,
        Forge,
        Fabric,
        Quilt,
        Liteloader,
        NeoForge,
        CleanRoom
    }
}
