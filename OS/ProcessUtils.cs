using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CSUtil.OS
{
    public static class ProcessUtils
    {
        private static string FindIndexedProcessName(int pid)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("Platform not supported");
            
            var processName = Process.GetProcessById(pid).ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexdName = null;

            for (var index = 0; index < processesByName.Length; index++) {
                processIndexdName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                if ((int) processId.NextValue() == pid) {
                    return processIndexdName;
                }
            }

            return processIndexdName;
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName) {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("Platform not supported");
            
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            return Process.GetProcessById((int) parentId.NextValue());
        }

        public static Process Parent(this Process process) {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }
        
        public static async Task<string> RunShellCommand(string cmd, bool redirectOutput = true)
        {
            Process p = new Process();
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    p.StartInfo.FileName = "sh";
                    p.StartInfo.Arguments = $"-c \"{cmd}\"";
                    break;

                case PlatformID.Win32Windows:
                case PlatformID.Win32S:
                case PlatformID.Win32NT:
                    p.StartInfo.FileName = "cmd";
                    p.StartInfo.Arguments = $"/C \"{cmd}\"";
                    break;

                default:
                    throw new NotSupportedException("Platform not supported");
            }

            if (redirectOutput)
            {
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
            }

            p.Start();
            await p.WaitForExitAsync();

            if (redirectOutput)
                return await p.StandardOutput.ReadToEndAsync();

            return "";
        }

        // Works only on linux
        public static async Task<string> GetParentName()
        {
            if (OperatingSystem.IsLinux())
                return await RunShellCommand("ps -e | grep $(ps -o ppid= -p $(echo $PPID))");
            
            if (OperatingSystem.IsWindows())
                return Process.GetCurrentProcess().Parent().ProcessName;

            throw new NotSupportedException("Platform not supported");
        }

        public static async void _RunIfNotAvalonia(Action runFunc)
        {
#if DEBUG
            var parentName = await GetParentName();
            if (parentName.Contains("java"))
                return;
#endif

            runFunc();
        }
    }
}