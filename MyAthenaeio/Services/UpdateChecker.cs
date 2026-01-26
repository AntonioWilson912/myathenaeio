using System.Reflection;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using Serilog;
using MyAthenaeio.Utils;

namespace MyAthenaeio.Services
{
    public class UpdateChecker
    {
        private static readonly ILogger _logger = Log.ForContext<BackupService>();
        private const string GITHUB_API_URL = "https://api.github.com/repos/AntonioWilson912/myAthenaeio/releases/latest";
        private static readonly HttpClient _httpClient = new();

        static UpdateChecker()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "myAthenaeio-UpdateChecker");
        }

        public static async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                var currentVersion = GetCurrentVersion();
                var latestRelease = await GetLatestReleaseAsync();

                if (latestRelease == null)
                    return null;

                var latestVersion = ParseVersion(latestRelease.TagName);

                if (latestVersion > currentVersion)
                {
                    return new UpdateInfo
                    {
                        IsUpdateAvailable = true,
                        CurrentVersion = currentVersion.ToShortString(),
                        LatestVersion = latestVersion.ToShortString(),
                        DownloadUrl = latestRelease.HtmlUrl,
                        ReleaseNotes = latestRelease.Body
                    };
                }

                return new UpdateInfo
                {
                    IsUpdateAvailable = false,
                    CurrentVersion = currentVersion.ToShortString(),
                    LatestVersion = latestVersion.ToShortString()
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking for updates");
                return null;
            }
        }

        private static Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version ?? new Version(0, 0, 0);
        }

        private static async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(GITHUB_API_URL);
                var reader = new JsonTextReader(new StringReader(response));
                var serializer = new JsonSerializer();

                return serializer.Deserialize<GitHubRelease>(reader);
            }
            catch
            {
                return null;
            }
        }

        private static Version ParseVersion(string tagName)
        {
            // Remove leading 'v' if present
            var versionString = tagName.TrimStart('v');

            if (Version.TryParse(versionString, out var version))
            {
                return version;
            }

            return new Version(0, 0, 0);
        }

        private class GitHubRelease
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; } = string.Empty;

            [JsonProperty("body")]
            public string Body { get; set; } = string.Empty;
        }
    }

    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
    }
}
