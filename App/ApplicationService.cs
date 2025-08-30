using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PCL.Core.App;

[LifecycleService(LifecycleState.BeforeLoading, Priority = int.MinValue)]
public sealed class ApplicationService() : GeneralService("application", "应用程序", false)
{
    public static Func<Application>? Loading { private get; set; }

    public override void Start()
    {
        ServiceContext.Debug("正在初始化 WPF 应用程序容器");
        var app = Loading!.Invoke();
        app.DispatcherUnhandledException += (_, e) => Lifecycle.OnException(e.Exception);
        app.Startup += (_, _) => Lifecycle.OnLoading();
        Lifecycle.CurrentApplication = app;
        Loading = null;
        ServiceContext.Trace("应用程序容器初始化完毕");
    }

    public override void Stop()
    {
        var app = Lifecycle.CurrentApplication;
        var dispatcher = app.Dispatcher;
        if (Lifecycle.IsForceShutdown)
        {
            ServiceContext.Warn("已指定强制关闭，跳过 WPF 标准关闭流程");
            return;
        }
        if (dispatcher == null || dispatcher.HasShutdownFinished) return;
        using var exited = new ManualResetEventSlim();
        dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
        {
            app.Exit += Exited;
            if (dispatcher.HasShutdownStarted) return;
            ServiceContext.Debug("发起 WPF 退出流程");
            app.Shutdown();
        });
        try
        {
            ServiceContext.Debug("正在等待应用程序容器退出");
            var result = exited.Wait(5000);
            if (result) ServiceContext.Trace("应用程序容器已退出");
            else ServiceContext.Warn("应用程序容器退出超时，停止等待");
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
