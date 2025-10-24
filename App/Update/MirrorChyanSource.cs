using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PCL.Core.ProgramSetup;

namespace PCL.Core.App.Update;

public class MirrorChyanSource(string cdk) : IUpdateSource
{
    private readonly string _cdk = cdk.Trim();

    public string Name { get => "Mirror Chyan"; }

    public HashSet<SourceAbility> Abilities { get => string.IsNullOrWhiteSpace(_cdk) ? [] : [SourceAbility.Update]; }

    public string BaseUrl
    {
        get
        {
            var archName = Basics.DeviceArchitecture.ToString().ToLower();
            var channelName = Setup.System.UpdateBranch == 1 ? "beta" : "stable";
            return
                $"https://mirrorchyan.com/api/resources/PCL2-CE/latest?cdk={_cdk}&os=win&arch={archName}&channel={channelName}";
        }
    }
}