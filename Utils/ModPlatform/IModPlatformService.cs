using System.Collections.Generic;
using System.Threading.Tasks;
using PCL.Core.Model.Mod;

namespace PCL.Core.Utils.ModPlatform;

public interface IModPlatformService
{
    Task<List<ModSearchResult>> Search(ModSearch query);
    Task<ModProjectDetail> GetProject(string id);
    Task<List<ModUpdateCheckResult>> CheckUpdatesFromHashes(IEnumerable<string> fileHashes);
}