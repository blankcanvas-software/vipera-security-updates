using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ViperaSecurity.Services
{
    public class DomainBlocklistManager
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ViperaSecurity"
        );
        private static readonly string BlocklistFile = Path.Combine(AppDataDir, "domain_blocklist.txt");
        private static readonly string UrlhausDomainsUrl = "https://urlhaus.abuse.ch/downloads/domains/";

        private HashSet<string>? _cachedDomains;
        private readonly HttpClient _httpClient;

        public int DomainCount => _cachedDomains?.Count ?? LoadFromDisk().Count;

        public DomainBlocklistManager()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Vipera-Security-Windows/2.0");
        }

        public HashSet<string> LoadFromDisk()
        {
            if (_cachedDomains != null) return _cachedDomains;

            var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                if (File.Exists(BlocklistFile))
                {
                    var lines = File.ReadAllLines(BlocklistFile);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim().ToLowerInvariant();
                        if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#"))
                        {
                            domains.Add(trimmed);
                        }
                    }
                }
                else
                {
                    // Default fallback blocklist entries if offline
                    var defaultBlocklist = new[]
                    {
                        "malware.testing.com",
                        "phishing.badsite.org",
                        "evil-tracker.net",
                        "cryptominer.pool.xyz",
                        "bad-actor-domain.com"
                    };
                    foreach (var d in defaultBlocklist) domains.Add(d);
                    File.WriteAllLines(BlocklistFile, domains);
                }
            }
            catch
            {
                // Fallback
            }

            _cachedDomains = domains;
            return domains;
        }

        public bool IsBlocked(string domain)
        {
            var domains = _cachedDomains ?? LoadFromDisk();
            string normalized = domain.Trim().ToLowerInvariant().TrimEnd('.');

            string current = normalized;
            while (current.Contains('.'))
            {
                if (domains.Contains(current)) return true;
                int dotIndex = current.IndexOf('.');
                if (dotIndex < 0) break;
                current = current.Substring(dotIndex + 1);
            }
            return false;
        }

        public async Task<int> RefreshAsync()
        {
            try
            {
                var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string response = await _httpClient.GetStringAsync(UrlhausDomainsUrl);

                using (var reader = new StringReader(response))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        string trimmed = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                            continue;

                        if (trimmed.Contains('.') && !trimmed.Contains(' '))
                        {
                            domains.Add(trimmed.ToLowerInvariant());
                        }
                    }
                }

                if (domains.Count > 0)
                {
                    if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);
                    await File.WriteAllLinesAsync(BlocklistFile, domains);
                    _cachedDomains = domains;
                    return domains.Count;
                }
            }
            catch
            {
                // Fallback to local cache count
            }
            return DomainCount;
        }

        private const string HostsHeader = "# VIPERA SECURITY WEB SHIELD BLOCKLIST START";
        private const string HostsFooter = "# VIPERA SECURITY WEB SHIELD BLOCKLIST END";

        public bool ApplySystemHostsProtection(bool enable)
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                if (!File.Exists(hostsPath)) return false;

                var existingLines = File.ReadAllLines(hostsPath).ToList();
                var cleanLines = new List<string>();
                bool inViperaSection = false;

                foreach (var line in existingLines)
                {
                    if (line.Trim() == HostsHeader)
                    {
                        inViperaSection = true;
                        continue;
                    }
                    if (line.Trim() == HostsFooter)
                    {
                        inViperaSection = false;
                        continue;
                    }
                    if (!inViperaSection)
                    {
                        cleanLines.Add(line);
                    }
                }

                if (enable)
                {
                    cleanLines.Add(HostsHeader);
                    var blocklist = LoadFromDisk().Take(500); // Sinkhole top malware/phishing domains to 127.0.0.1
                    foreach (var domain in blocklist)
                    {
                        cleanLines.Add($"127.0.0.1\t{domain}");
                        if (!domain.StartsWith("www."))
                        {
                            cleanLines.Add($"127.0.0.1\twww.{domain}");
                        }
                    }
                    cleanLines.Add(HostsFooter);
                }

                File.WriteAllLines(hostsPath, cleanLines);
                FlushDnsCache();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void FlushDnsCache()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi)?.WaitForExit(3000);
            }
            catch { }
        }
    }
}
