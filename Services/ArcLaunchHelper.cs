using System.IO;

namespace SusanooLauncher.Services
{
    internal static class ArcLaunchHelper
    {
        internal static bool Is64BitPe(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                if (fs.Length < 0x200)
                    return false;

                Span<byte> dos = stackalloc byte[64];
                fs.Read(dos);
                if (dos[0] != (byte)'M' || dos[1] != (byte)'Z')
                    return false;

                int peOffset = BitConverter.ToInt32(dos.Slice(60, 4));
                if (peOffset <= 0 || peOffset + 6 > fs.Length)
                    return false;

                fs.Seek(peOffset + 4, SeekOrigin.Begin);
                Span<byte> machine = stackalloc byte[2];
                fs.Read(machine);
                return machine[0] == 0x64 && machine[1] == 0x86;
            }
            catch
            {
                return false;
            }
        }

        internal static string? FindBuiltArcPath()
        {
            foreach (string path in LauncherDownloads.EnumerateArcCandidatePaths())
            {
                if (!path.EndsWith("Arc.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!path.Contains(@"\x64\Release\", StringComparison.OrdinalIgnoreCase)
                    && !path.Contains(@"\build\", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (File.Exists(path) && Is64BitPe(path))
                    return path;
            }

            return null;
        }

        internal static bool DeployBuiltArc(string targetArcPath)
        {
            string? built = FindBuiltArcPath();
            if (built == null)
                return false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetArcPath)!);
                File.Copy(built, targetArcPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
