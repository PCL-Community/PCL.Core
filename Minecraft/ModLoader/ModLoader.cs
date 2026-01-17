using System.Text.Json.Nodes;

namespace PCL.Core.Minecraft.ModLoader;

public abstract class ModLoader
{
    public abstract McVersionRanges VersionRanges { get; }
    public abstract string Name { get; }
    public abstract string Version { get; }
    public abstract string Icon { get; }

    public virtual void MergeJson(ref JsonNode node) { }
    public virtual void MergeVersionName(ref string name) { }

    public virtual void PreInstallTask() { }
    public virtual void PostInstallTask() { }
    public virtual void UninstallTask(ref JsonNode node, ref string name) { }
}