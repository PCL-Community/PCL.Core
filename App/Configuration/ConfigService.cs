namespace PCL.Core.App.Configuration;

[LifecycleService(LifecycleState.Loading, Priority = 1919810)]
public sealed class ConfigService() : GeneralService("config", "配置服务")
{
    // TODO get set read write
}
