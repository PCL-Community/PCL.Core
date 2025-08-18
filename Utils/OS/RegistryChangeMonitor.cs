using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace PCL.Core.Utils.OS;

public class RegistryChangeMonitor : IDisposable
{
    // ReSharper disable once InconsistentNaming
    private const int REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;
    // ReSharper disable once InconsistentNaming
    private const int KEY_NOTIFY = 0x0010;
    // ReSharper disable once InconsistentNaming
    private const int KEY_QUERY_VALUE = 0x0001;
    // ReSharper disable once InconsistentNaming
    private const int KEY_READ = (KEY_QUERY_VALUE | KEY_NOTIFY);
    // ReSharper disable once InconsistentNaming
    private static readonly UIntPtr HKEY_CURRENT_USER = (UIntPtr)0x80000001;

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegOpenKeyEx(UIntPtr hKey, string subKey, uint options, int samDesired, out IntPtr phkResult);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, int dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegCloseKey(IntPtr hKey);

    private readonly IntPtr _hKey;
    private readonly ManualResetEvent _stopEvent = new(false);
    private readonly ManualResetEvent _registryEvent = new(false);
    private readonly Thread _monitorThread;

    public event EventHandler? Changed;

    public RegistryChangeMonitor(string keyPath)
    {
        // Open registry key with proper access rights
        var result = RegOpenKeyEx(HKEY_CURRENT_USER, keyPath, 0, KEY_READ, out _hKey);
        if (result != 0) throw new Win32Exception(result);

        // Start monitoring thread
        _monitorThread = new Thread(_MonitorThread) { IsBackground = true };
        _monitorThread.Start();
    }

    private void _MonitorThread()
    {
        try
        {
            // Initial registration
            _RegisterForNotification();

            while (!_stopEvent.WaitOne(0))
            {
                // Wait for either registry change or stop signal
                var index = WaitHandle.WaitAny(
                    [_registryEvent, _stopEvent],
                    TimeSpan.FromSeconds(1)); // Timeout to check for stop periodically

                if (index == 1) break; // Stop requested

                if (index == 0)
                {
                    _registryEvent.Reset();
                    Changed?.Invoke(this, EventArgs.Empty);
                    _RegisterForNotification(); // Re-register for next change
                }
            }
        }
        finally
        {
            _registryEvent.Dispose();
        }
    }

    private void _RegisterForNotification()
    {
        var result = RegNotifyChangeKeyValue(
            _hKey,
            true,
            REG_NOTIFY_CHANGE_LAST_SET,
            _registryEvent.SafeWaitHandle.DangerousGetHandle(),
            true); // Must be asynchronous to allow graceful shutdown

        if (result != 0)
        {
            // Handle error - key might have been deleted
            _stopEvent.Set();
            throw new Win32Exception(result);
        }
    }

    public void Dispose()
    {
        _stopEvent.Set();

        // Give thread a chance to exit gracefully
        if (_monitorThread is {IsAlive: true})
            _monitorThread.Join(1000);

        if (_hKey != IntPtr.Zero)
            _ = RegCloseKey(_hKey);

        _stopEvent.Dispose();
        GC.SuppressFinalize(this);
    }
}