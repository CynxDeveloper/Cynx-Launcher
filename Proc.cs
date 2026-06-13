using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SusanooLauncher
{
    internal static class Proc
    {
        [DllImport("kernel32.dll")]
        private static extern nint OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        private const int ThreadSuspendResume = 0x0002;

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(nint hThread);

        [DllImport("kernel32.dll")]
        private static extern uint ResumeThread(nint hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(nint hObject);

        public static void Suspend(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                nint num = OpenThread(2, bInheritHandle: false, (uint)thread.Id);
                if (num == IntPtr.Zero)
                {
                    break;
                }
                SuspendThread(num);
            }
        }

        public static void Resume(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                nint handle = OpenThread(ThreadSuspendResume, bInheritHandle: false, (uint)thread.Id);
                if (handle == IntPtr.Zero)
                    continue;

                ResumeThread(handle);
                CloseHandle(handle);
            }
        }

        public static Process? Start(string Base, string path, string args = "", bool suspend = false, string? workingDirectory = null)
        {
            string fullPath = Path.Combine(Base, path);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                Arguments = args,
                WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(fullPath) ?? Base,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            if (process == null)
            {
                return null;
            }
            if (suspend)
            {
                process.Suspend();
            }
            return process;
        }
    }
}
