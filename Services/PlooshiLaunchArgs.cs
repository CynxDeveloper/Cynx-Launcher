namespace SusanooLauncher.Services
{
    /// <summary>Launch arguments from https://github.com/plooshi/Launcher</summary>
    internal static class PlooshiLaunchArgs
    {
        public const string EpicBase =
            "-epicapp=Fortnite -epicenv=Prod -epiclocale=en-us -epicportal -nobe -fromfl=eac -fltoken=h1cdhchd10150221h130eB56 -skippatchcheck";

        public const string Caldera =
            "-caldera=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY2NvdW50X2lkIjoiMTM5ZDAzOGFmOTM2NDcyODgxMTdlYWU3MWYxZGQ5ZTQiLCJnZW5lcmF0ZWQiOjE3MDQ0MTE5MDQsImNhbGRlcmFHdWlkIjoiODhjZmQ5NzYtM2U2OS00MWYzLWI2ODEtYzQyOTcxM2ZkMWFlIiwiYWNQcm92aWRlciI6IkVhc3lBbnRpQ2hlYXQiLCJub3RlcyI6IiIsImZhbGxiYWNrIjpmYWxzZX0.Q8hdxvrW2sH-3on6JEBLANB0rkPAGUwbZYPrCOMTtvA";

        public static string BuildShippingArguments(string authLogin, string authPassword, string authType, string backendUrl)
        {
            return $"{EpicBase} {Caldera} -AUTH_LOGIN={authLogin} -AUTH_PASSWORD={authPassword} -AUTH_TYPE={authType} -backend={backendUrl}";
        }

        public static string BuildHelperArguments(string backendUrl) =>
            $"{EpicBase} -backend={backendUrl}";
    }
}
