using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCL.Core.App.Update;

public interface IUpdateSource
{
    string Name { get; }
    HashSet<SourceAbility> Abilities { get; }
    string BaseUrl { get; }
}