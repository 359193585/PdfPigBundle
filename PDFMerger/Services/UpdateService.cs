using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace PdfMerger.Services
{
    public static class UpdateService
    {
        private const string RepoOwner = "359193585";
        private const string RepoName = "PdfMerger";

        public static async Task<string?> GetLatestVersionAsync()
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PDFMerger");
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            return tag?.TrimStart('v');
        }

        public static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null) return "1.0.0";
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public static int CompareVersions(string latest, string current)
        {
            var v1 = new Version(latest);
            var v2 = new Version(current);
            return v1.CompareTo(v2);
        }

        public static void OpenDownloadPage()
        {
            var url = $"https://github.com/{RepoOwner}/{RepoName}/releases/latest";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
}
