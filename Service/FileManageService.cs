using PCL.Core.LifecycleManagement;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.Loading, Priority = 10000)]
public class FileManageService : ILifecycleService
{
    public string Identifier => "file-manage";
    public string Name => "文件资源管理";
    public bool SupportAsyncStart => false;
    public void Start()
    {
        
    }

    public void Stop()
    {
        
    }
}