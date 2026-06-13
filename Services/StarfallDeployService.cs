using System.IO;
using System.Reflection;

namespace SusanooLauncher.Services
{
    /// <summary>Deploys embedded Starfall.dll like https://github.com/plooshi/Launcher</summary>
    internal static class StarfallDeployService
    {
        internal static bool TryDeploy(string fortniteRoot)
        {
            try
            {
                using Stream? source = OpenStarfallStream();
                if (source == null)
                    return false;

                string win64 = Path.Join(fortniteRoot, "Engine", "Binaries", "ThirdParty", "NVIDIA", "NVaftermath", "Win64");
                Directory.CreateDirectory(win64);

                string x64Path = Path.Join(win64, "GFSDK_Aftermath_Lib.x64.dll");
                string dllPath = Path.Join(win64, "GFSDK_Aftermath_Lib.dll");

                WriteStreamToFile(source, x64Path);
                File.Copy(x64Path, dllPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Stream? OpenStarfallStream()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string name in asm.GetManifestResourceNames())
            {
                if (name.Contains("starfall", StringComparison.OrdinalIgnoreCase))
                    return asm.GetManifestResourceStream(name);
            }

            return asm.GetManifestResourceStream("SusanooLauncher.Starfall.dll");
        }

        private static void WriteStreamToFile(Stream source, string path)
        {
            source.Position = 0;
            using FileStream dest = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            source.CopyTo(dest);
        }
    }
}
