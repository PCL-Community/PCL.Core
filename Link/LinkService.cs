using System;
using System.Collections.Generic;
using PCL.Core.App;
using PCL.Core.Link.Scaffolding;
using PCL.Core.Logging;

namespace PCL.Core.Link;

/// <summary>
/// 联机协议管理服务
/// </summary>
[LifecycleService(LifecycleState.Running)]
public class LinkService() : GeneralService("link", "联机服务")
{
    private readonly List<ILinkHelper> _linkHelpers = [];
    
    public override void Start()
    {
        _linkHelpers.Add(new ScfHelper());
    }
    
    public override void Stop()
    {
        Close();
        _linkHelpers.Clear();
    }

    public void Launch()
    {
        foreach (var helper in _linkHelpers)
        {
            if (helper.Launch() == 0)
            {
                LogWrapper.Info("Link", $"联机协议 {helper.Name} 启动成功");
            }
            else
            {
                LogWrapper.Error("Link", $"联机协议 {helper.Name} 启动失败");
            }
        }
    }
    public void Close()
    {
        foreach (var helper in _linkHelpers)
        {
            if (helper.Close() == 0)
            {
                LogWrapper.Info("Link", $"联机协议 {helper.Name} 关闭成功");
            }
            else
            {
                LogWrapper.Error("Link", $"联机协议 {helper.Name} 关闭失败");
            }
        }
    }
}