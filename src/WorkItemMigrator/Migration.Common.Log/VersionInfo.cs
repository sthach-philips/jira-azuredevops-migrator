using Newtonsoft.Json.Linq;
using Semver;
using System;
using System.Diagnostics;
using System.Net.Http;

namespace Migration.Common.Log
{
    public static class VersionInfo
    {
        public static string GetVersionInfo()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.ProductVersion;
            return version;
        }

        public static string GetCopyrightInfo()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.LegalCopyright;
        }

        public static void PrintInfoMessage(string app)
        {
            Console.WriteLine($"{app} v{GetVersionInfo()}");
            Console.WriteLine(GetCopyrightInfo());
            if (VersionInfo.NewerVersionExists(out var latestVersion))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Wow, there's a newer release out (v{latestVersion})! We recommend downloading it for latest features and fixes.");
                Console.ResetColor();
            }
        }

        private static bool NewerVersionExists(out string latestVersion)
        {
            var currentVersion = latestVersion = GetVersionInfo();

            try
            {
                latestVersion = GetLatestReleaseVersion();

                var latest = SemVersion.Parse(latestVersion, SemVersionStyles.Strict);
                var current = SemVersion.Parse(currentVersion, SemVersionStyles.Strict);
                if (latest.ComparePrecedenceTo(current) > 0)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static string GetLatestReleaseVersion()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");

                var response = httpClient.GetStringAsync(new Uri("https://api.github.com/repos/solidify/jira-azuredevops-migrator/releases/latest")).Result;

                var o = JObject.Parse(response);
                var ver = o.SelectToken("$.name").Value<string>();

                return ver.Replace("v", "");
            }
        }
    }
}
