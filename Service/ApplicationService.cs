using PCL.Core.LifecycleManagement;
using System;
using System.Windows;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.BeforeLoading, Priority = int.MinValue)]
public sealed class ApplicationService : ILifecycleService
{
    public static Func<Application>? Loading { private get; set; }

    public string Identifier => "application";
    public string Name => "应用程序";
    public bool SupportAsyncStart => false;

    private readonly LifecycleContext Context;
    private ApplicationService() { Context = Lifecycle.GetContext(this); }
    
    public void Start()
    {
        Context.Debug("正在初始化 WPF 应用程序容器");
        var app = Loading!.Invoke();
        app.DispatcherUnhandledException += (_, e) => Lifecycle.OnException(e.Exception);
        app.Startup += (_, _) => Lifecycle.OnLoading();
        Lifecycle.CurrentApplication = app;
        Context.Trace("应用程序容器初始化完毕");
    }

    public void Stop() { }
}
