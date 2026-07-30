using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ViperaSecurity.Services
{
    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string LatestVersion { get; set; } = "1.2.0";
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
    }

    public class UpdateService
    {
        public static string GetInstalledVersion()
        {
            try
            {
                string appDataVersion = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ViperaSecurity", "version.txt");
                if (File.Exists(appDataVersion))
                {
                    string v = File.ReadAllText(appDataVersion).Trim();
                    if (!string.IsNullOrEmpty(v)) return v;
                }

                string versionFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (File.Exists(versionFile))
                {
                    string v = File.ReadAllText(versionFile).Trim();
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            catch { }
            return "1.2.0";
        }

        public string CurrentVersion => GetInstalledVersion();

        private readonly SupabaseService _supabaseService;
        private readonly HttpClient _httpClient;

        public UpdateService(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Vipera-Security-Updater/2.0");
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            var result = new UpdateInfo();
            string currentVer = GetInstalledVersion();

            // 1. Check live GitHub version.txt on main branch
            try
            {
                string ghVerUrl = "https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/src/ViperaSecurity/version.txt";
                var ghResp = await _httpClient.GetAsync(ghVerUrl);
                if (ghResp.IsSuccessStatusCode)
                {
                    string ghVerText = (await ghResp.Content.ReadAsStringAsync()).Trim();
                    if (!string.IsNullOrEmpty(ghVerText) && Version.TryParse(ghVerText, out var remoteVer) && Version.TryParse(currentVer, out var localVer))
                    {
                        if (remoteVer > localVer)
                        {
                            result.IsUpdateAvailable = true;
                            result.LatestVersion = ghVerText;
                            result.DownloadUrl = "https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip";
                            result.ReleaseNotes = $"Vipera Security v{ghVerText} - Startup Auto-Update & Hourly Background Scanner Engine";
                            result.IsMandatory = false;
                            return result;
                        }
                    }
                }
            }
            catch { }

            // 2. Fallback to Supabase app_versions table
            try
            {
                string requestUrl = $"{_supabaseService.BaseUrl.TrimEnd('/')}/rest/v1/app_versions?select=version,download_url,release_notes,is_mandatory&order=created_at.desc&limit=1";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("apikey", SupabaseService.PublishableKey);
                request.Headers.Add("Authorization", $"Bearer {SupabaseService.PublishableKey}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var latest = doc.RootElement[0];
                        string latestVer = latest.GetProperty("version").GetString() ?? currentVer;
                        string downloadUrl = latest.GetProperty("download_url").GetString() ?? string.Empty;
                        string notes = latest.TryGetProperty("release_notes", out var n) ? (n.GetString() ?? "") : "";
                        bool mandatory = latest.TryGetProperty("is_mandatory", out var m) && m.GetBoolean();

                        if (Version.TryParse(latestVer, out var remoteVer) && Version.TryParse(currentVer, out var localVer))
                        {
                            if (remoteVer > localVer)
                            {
                                result.IsUpdateAvailable = true;
                                result.LatestVersion = latestVer;
                                result.DownloadUrl = downloadUrl;
                                result.ReleaseNotes = notes;
                                result.IsMandatory = mandatory;
                            }
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        public async Task<bool> DownloadAndApplyUpdateAsync(string downloadUrl, string targetVersion, Action<double, string> progressCallback)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ViperaSecurityUpdate");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
                Directory.CreateDirectory(tempDir);

                string zipPath = Path.Combine(tempDir, "update.zip");
                string extractedDir = Path.Combine(tempDir, "extracted");

                progressCallback(10, "Downloading server update package...");
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? 1;

                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    byte[] buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        double pct = 10.0 + ((double)totalRead / totalBytes) * 60.0;
                        progressCallback(pct, $"Downloading: {totalRead / 1024} KB / {totalBytes / 1024} KB");
                    }
                }

                progressCallback(75, "Extracting update package...");
                ZipFile.ExtractToDirectory(zipPath, extractedDir);

                // Save version.txt to AppData for persistent tracking
                if (!string.IsNullOrEmpty(targetVersion))
                {
                    File.WriteAllText(Path.Combine(extractedDir, "version.txt"), targetVersion);
                    try
                    {
                        string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ViperaSecurity");
                        if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
                        File.WriteAllText(Path.Combine(appDataDir, "version.txt"), targetVersion);
                    }
                    catch { }
                }

                progressCallback(90, "Preparing silent updater script...");
                string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                string currentAppDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');

                string batchScript = Path.Combine(tempDir, "apply_update.bat");
                string srcPattern = Path.Combine(extractedDir, "*");
                string scriptContent =
                    "@echo off\r\n" +
                    ":retry_kill\r\n" +
                    "taskkill /f /im ViperaSecurity.exe >nul 2>&1\r\n" +
                    "timeout /t 1 /nobreak > nul\r\n" +
                    "tasklist /fi \"IMAGENAME eq ViperaSecurity.exe\" 2>NUL | find /I /N \"ViperaSecurity.exe\">NUL\r\n" +
                    "if %ERRORLEVEL%==0 goto retry_kill\r\n" +
                    $@"xcopy /s /e /y ""{srcPattern}"" ""{currentAppDir}"" " + "\r\n" +
                    $@"start """" ""{currentExe}"" " + "\r\n" +
                    $@"rmdir /s /q ""{tempDir}"" " + "\r\n" +
                    "exit\r\n";
                File.WriteAllText(batchScript, scriptContent);

                progressCallback(100, "Restarting Vipera Security with update applied!");

                var psi = new ProcessStartInfo
                {
                    FileName = batchScript,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                try
                {
                    Process.Start(psi);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // User canceled UAC prompt
                    psi.Verb = "";
                    Process.Start(psi);
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                });

                return true;
            }
            catch (Exception ex)
            {
                progressCallback(0, $"Update Failed: {ex.Message}");
                return false;
            }
        }
    }
}
