using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ViperaSecurity.Services;

namespace ViperaSecurity.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly UpdateService _updateService;
        private readonly LocalizationService _loc = LocalizationService.Instance;

        [ObservableProperty]
        private string _selectedLanguage = "en";

        [ObservableProperty]
        private string _selectedAutoScanSchedule = "Daily";

        [ObservableProperty]
        private bool _isWebProtectionEnabled;

        [ObservableProperty]
        private bool _isRealTimeShieldEnabled;

        [ObservableProperty]
        private bool _isAutoScanEnabled;

        [ObservableProperty]
        private bool _isAutoUpdateOnStartupEnabled;

        [ObservableProperty]
        private string _saveMessage = string.Empty;

        [ObservableProperty]
        private string _appVersionText = "v1.0.0";

        [ObservableProperty]
        private string _updateStatusText = "App is up to date.";

        [ObservableProperty]
        private bool _isUpdateAvailable;

        [ObservableProperty]
        private bool _isDownloadingUpdate;

        [ObservableProperty]
        private double _updateProgress;

        private string _downloadUrl = string.Empty;
        private string _targetVersion = string.Empty;

        public List<string> AvailableLanguages { get; } = new() { "English (en)", "French (fr)", "German (de)", "Spanish (es)" };
        public List<string> AutoScanOptions { get; } = new() { "Hourly (1 hr)", "Daily", "Weekly", "On Startup", "Disabled" };

        public SettingsViewModel(ISettingsService settingsService, UpdateService updateService)
        {
            _settingsService = settingsService;
            _updateService = updateService;

            SelectedLanguage = _settingsService.Settings.Language switch
            {
                "fr" => "French (fr)",
                "de" => "German (de)",
                "es" => "Spanish (es)",
                _ => "English (en)"
            };
            SelectedAutoScanSchedule = _settingsService.Settings.AutoScanSchedule;
            IsWebProtectionEnabled = _settingsService.Settings.WebProtectionEnabled;
            IsRealTimeShieldEnabled = _settingsService.Settings.RealTimeShieldEnabled;
            IsAutoScanEnabled = _settingsService.Settings.AutoScanEnabled;
            IsAutoUpdateOnStartupEnabled = _settingsService.Settings.AutoUpdateOnStartup;

            AppVersionText = $"v{UpdateService.GetInstalledVersion()}";
        }

        [RelayCommand]
        public async Task CheckForUpdates()
        {
            UpdateStatusText = "Checking server for new updates...";
            var info = await _updateService.CheckForUpdatesAsync();

            if (info.IsUpdateAvailable)
            {
                IsUpdateAvailable = true;
                _downloadUrl = info.DownloadUrl;
                _targetVersion = info.LatestVersion;
                UpdateStatusText = $"🚀 New Version v{info.LatestVersion} Available!";
            }
            else
            {
                IsUpdateAvailable = false;
                UpdateStatusText = $"✓ Vipera Security v{UpdateService.GetInstalledVersion()} is running the latest version.";
            }
        }

        [RelayCommand]
        public async Task ApplyUpdate()
        {
            if (string.IsNullOrEmpty(_downloadUrl))
            {
                UpdateStatusText = "No update URL available.";
                return;
            }

            IsDownloadingUpdate = true;
            UpdateProgress = 0;

            await _updateService.DownloadAndApplyUpdateAsync(_downloadUrl, _targetVersion, (pct, status) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateProgress = pct;
                    UpdateStatusText = status;
                });
            });
        }

        [RelayCommand]
        public void SaveSettings()
        {
            string code = SelectedLanguage.Contains("(fr)") ? "fr"
                : SelectedLanguage.Contains("(de)") ? "de"
                : SelectedLanguage.Contains("(es)") ? "es" : "en";

            _settingsService.Settings.Language = code;
            _loc.CurrentLanguage = code;

            _settingsService.Settings.AutoScanSchedule = SelectedAutoScanSchedule;
            _settingsService.Settings.WebProtectionEnabled = IsWebProtectionEnabled;
            _settingsService.Settings.RealTimeShieldEnabled = IsRealTimeShieldEnabled;
            _settingsService.Settings.AutoScanEnabled = IsAutoScanEnabled;
            _settingsService.Settings.AutoUpdateOnStartup = IsAutoUpdateOnStartupEnabled;
            _settingsService.Save();

            SaveMessage = "Settings updated successfully!";
        }
    }
}
