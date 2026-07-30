using System;
using System.Collections.Generic;

namespace ViperaSecurity.Models
{
    public enum ThreatStatus
    {
        Clean,
        Suspicious,
        Malicious,
        Quarantined,
        Deleted,
        Ignored
    }

    public class ScanFileItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string FilePath => Path;
        public string Sha256 { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string Extension { get; set; } = string.Empty;
        public ThreatStatus Status { get; set; } = ThreatStatus.Clean;
        public string ThreatName { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; } = DateTime.Now;

        public string Severity => Status switch
        {
            ThreatStatus.Malicious => "CRITICAL RISK",
            ThreatStatus.Suspicious => "SUSPICIOUS",
            ThreatStatus.Quarantined => "QUARANTINED",
            ThreatStatus.Deleted => "DELETED",
            _ => "CLEAN"
        };

        public string RiskColor => Status switch
        {
            ThreatStatus.Malicious => "#EF4444",
            ThreatStatus.Suspicious => "#F59E0B",
            ThreatStatus.Quarantined => "#10B981",
            _ => "#64748B"
        };

        public string HumanDescription => Status switch
        {
            ThreatStatus.Malicious => $"Malicious threat detected ({ThreatName}). Recommended action: Quarantine or Delete immediately.",
            ThreatStatus.Suspicious => "File exhibits suspicious executable behavior or double-extension naming. Proceed with caution.",
            ThreatStatus.Quarantined => "File is safely locked in isolated storage container.",
            ThreatStatus.Deleted => "File permanently removed from system.",
            _ => "No threat signature found."
        };

        public string FormattedSize
        {
            get
            {
                if (SizeBytes < 1024) return $"{SizeBytes} B";
                if (SizeBytes < 1024 * 1024) return $"{SizeBytes / 1024.0:F1} KB";
                return $"{SizeBytes / (1024.0 * 1024.0):F1} MB";
            }
        }
    }

    public class ThreatLookupResult
    {
        public bool MalwareKnown { get; set; }
        public bool OtxKnown { get; set; }
        public bool MetaKnown { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string? AiVerdict { get; set; }
    }

    public class UrlLookupResult
    {
        public string Url { get; set; } = string.Empty;
        public bool IsMalicious { get; set; }
        public string Summary { get; set; } = string.Empty;
        public bool UrlhausThreat { get; set; }
        public bool SafeBrowsingThreat { get; set; }
    }

    public class SystemHealthInfo
    {
        public bool IsDefenderActive { get; set; } = true;
        public bool IsFirewallActive { get; set; } = true;
        public bool IsUacEnabled { get; set; } = true;
        public double CpuUsagePercent { get; set; }
        public double RamUsagePercent { get; set; }
        public double TotalRamGb { get; set; }
        public double FreeDiskGb { get; set; }
        public string HealthStatus { get; set; } = "Good";
        public string Summary { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        public string Language { get; set; } = "en";
        public bool WebProtectionEnabled { get; set; } = true;
        public bool RealTimeShieldEnabled { get; set; } = true;
        public bool AutoScanEnabled { get; set; } = true;
        public int AutoScanIntervalHours { get; set; } = 1;
        public bool AutoUpdateOnStartup { get; set; } = true;
        public string AutoScanSchedule { get; set; } = "Hourly (1 hr)";
        public string LicenseKey { get; set; } = string.Empty;
        public bool IsPremium { get; set; }
        public DateTime ProSubscriptionExpiry { get; set; } = DateTime.MinValue;
        public DateTime LastScanTime { get; set; } = DateTime.MinValue;
        public int TotalScansCompleted { get; set; }
        public int ThreatsBlocked { get; set; }
    }
}
