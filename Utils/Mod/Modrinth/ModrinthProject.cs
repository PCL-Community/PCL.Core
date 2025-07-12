using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.Model.Mod.Modrinth;
using PCL.Core.Utils.Minecraft;

namespace PCL.Core.Utils.Mod.Modrinth;

public class ModrinthProject : IProject
{
    private ModrinthProjectModel _model;

    public string Slug
    {
        get => _model.slug;
    }

    public string Name
    {
        get => _model.title;
    }

    public string Description
    {
        get => _model.description;
    }

    public HashSet<string> SupportedGameVersions
    {
        get => _model.versions.ToHashSet();
    }

    public HashSet<ModLoaders> SupportedLoaders
    {
        get => _model.loaders.ToHashSet();
    }

    public async Task<List<IProjectFile>> GetFiles()
    {
        throw new System.NotImplementedException();
    }
}