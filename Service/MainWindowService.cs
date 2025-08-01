using System;
using System.Windows;
using PCL.Core.LifecycleManagement;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.WindowCreating, Priority = int.MaxValue)]
public sealed class MainWindowService : ILifecycleService
{
    public static Func<Window>? Loading { private get; set; }
    
    public string Identifier => "window";
    public string Name => "主窗体";
    public bool SupportAsyncStart => false;

    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private MainWindowService() { _context = Lifecycle.GetContext(this); }
    
    public void Start()
    {
        Context.Debug("正在初始化 WPF 窗体");
        var window = Loading!.Invoke();
        window.Loaded += (_, _) => Lifecycle.OnWindowCreated();
        Lifecycle.CurrentApplication.MainWindow = window;
        Context.Trace("窗体创建完毕");
    }

    public void Stop() { }
}
