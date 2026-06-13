using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace SusanooLauncher.Services
{
    internal static class HardwareIdService
    {
        private static string? _cached;

        internal static string GetMachineId()
        {
            if (!string.IsNullOrEmpty(_cached))
                return _cached;

            string machineGuid = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography",
                "MachineGuid",
                "")?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(machineGuid))
                machineGuid = Environment.MachineName;

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(machineGuid));
            _cached = Convert.ToHexString(hash).ToLowerInvariant();
            return _cached;
        }
    }
}
