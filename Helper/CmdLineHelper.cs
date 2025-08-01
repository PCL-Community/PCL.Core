using System.Management;

namespace PCL.Core.Helper
{
    public static class CmdLineHelper
    {
        public static string GetCommandLine(int pid)
        {
            string query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {pid}";
            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject obj in searcher.Get())
                    return obj["CommandLine"]?.ToString();
            }
            return null;
        }
    }
}
