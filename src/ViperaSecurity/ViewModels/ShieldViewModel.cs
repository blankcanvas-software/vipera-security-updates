using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ViperaSecurity.Models;
using ViperaSecurity.Services;

namespace ViperaSecurity.ViewModels
{
    public partial class ShieldViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly DomainBlocklistManager _blocklistManager;
        private readonly UrlLookupClient _urlLookupClient;

        [ObservableProperty]
        private bool _isWebShieldActive;

        [ObservableProperty]
        private int _blockedDomainsCount;

        [ObservableProperty]
        private string _blocklistCountText = string.Empty;

        [ObservableProperty]
        private string _testUrlInput = string.Empty;

        [ObservableProperty]
        private bool _isCheckingUrl;

        [ObservableProperty]
        private UrlLookupResult? _urlResult;

        [ObservableProperty]
        private string _lookupResultText = "Enter any URL or domain name above and click 'Run Threat Verdict' to test Web Shield live intelligence.";

        public ShieldViewModel(ISettingsService settingsService, DomainBlocklistManager blocklistManager, UrlLookupClient urlLookupClient)
        {
            _settingsService = settingsService;
            _blocklistManager = blocklistManager;
            _urlLookupClient = urlLookupClient;

            IsWebShieldActive = _settingsService.Settings.WebProtectionEnabled;
            BlockedDomainsCount = _blocklistManager.DomainCount;
            BlocklistCountText = $"{BlockedDomainsCount:N0} Malicious Domains Active";

            // Automatically enforce host protection if active on startup
            if (IsWebShieldActive)
            {
                Task.Run(() => _blocklistManager.ApplySystemHostsProtection(true));
            }
        }

        partial void OnIsWebShieldActiveChanged(bool value)
        {
            _settingsService.Settings.WebProtectionEnabled = value;
            _settingsService.Save();
            Task.Run(() => _blocklistManager.ApplySystemHostsProtection(value));
        }

        [RelayCommand]
        public async Task RefreshBlocklist()
        {
            BlockedDomainsCount = await _blocklistManager.RefreshAsync();
            BlocklistCountText = $"{BlockedDomainsCount:N0} Malicious Domains Active";
            if (IsWebShieldActive)
            {
                await Task.Run(() => _blocklistManager.ApplySystemHostsProtection(true));
            }
        }

        [RelayCommand]
        public async Task TestUrl()
        {
            if (string.IsNullOrWhiteSpace(TestUrlInput))
            {
                LookupResultText = "Please enter a valid URL or domain to test (e.g. badsite.com).";
                return;
            }

            IsCheckingUrl = true;
            LookupResultText = "🔎 Querying URLhaus & Google Safe Browsing threat databases...";

            try
            {
                UrlResult = await _urlLookupClient.LookupUrlAsync(TestUrlInput);
                LookupResultText = UrlResult.Summary;
            }
            catch (Exception ex)
            {
                LookupResultText = $"⚠ Error verifying URL: {ex.Message}";
            }
            finally
            {
                IsCheckingUrl = false;
            }
        }
    }
}
