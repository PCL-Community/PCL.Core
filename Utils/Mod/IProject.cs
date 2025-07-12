using System.Collections.Generic;
using System.Threading.Tasks;
using PCL.Core.Utils.Minecraft;

namespace PCL.Core.Utils.Mod;

public interface IProject
{
    string Name { get; }
    string Slug { get; }
    string Description { get; }
    HashSet<string> SupportedGameVersions { get; }
    HashSet<ModLoaders> SupportedLoaders { get; }
    Task<List<IProjectFile>> GetFiles();
}