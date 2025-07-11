using PCL.Core.LifecycleManagement;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.BeforeLoading, Priority = int.MinValue)]
public sealed class ApplicationService : ILifecycleService
{
    public static Func<Application>? Loading { private get; set; }

    public string Identifier => "application";
    public string Name => "应用程序";
    public bool SupportAsyncStart => false;

    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private ApplicationService() { _context = Lifecycle.GetContext(this); }
    
    public void Start()
    {
        Context.Debug("正在初始化 WPF 应用程序容器");
        var app = Loading!.Invoke();
        app.DispatcherUnhandledException += (_, e) => Lifecycle.OnException(e.Exception);
        app.Startup += (_, _) => Lifecycle.OnLoading();
        Lifecycle.CurrentApplication = app;
        Loading = null;
        Context.Trace("应用程序容器初始化完毕");
    }

    public void Stop()
    {
        var app = Lifecycle.CurrentApplication;
        var dispatcher = app.Dispatcher;
        if (Lifecycle.IsForceShutdown)
        {
            Context.Warn("已指定强制关闭，跳过 WPF 标准关闭流程");
            return;
        }
        if (dispatcher == null || dispatcher.HasShutdownFinished) return;
        using var exited = new ManualResetEventSlim();
        dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
        {
            app.Exit += Exited;
            if (dispatcher.HasShutdownStarted) return;
            Context.Debug("发起 WPF 退出流程");
            app.Shutdown();
        });
        try
        {
            Context.Debug("正在等待应用程序容器退出");
            var result = exited.Wait(5000);
            if (result) Context.Trace("应用程序容器已退出");
            else Context.Warn("应用程序容器退出超时，停止等待");
        }
        finally
        {
            dispatcher.BeginInvoke(DispatcherPriority.Send, () => app.Exit -= Exited);
        }
        return;
        
        void Exited(object? sender, EventArgs e)
        {
            // ReSharper disable once AccessToDisposedClosure
            exited.Set();
        }
    }
}
