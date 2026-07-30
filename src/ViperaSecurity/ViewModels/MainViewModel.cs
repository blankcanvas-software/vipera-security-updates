using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ViperaSecurity.Models;
using ViperaSecurity.Services;

namespace ViperaSecurity.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IBillingService _billingService;
        private readonly LocalizationService _loc = LocalizationService.Instance;

        [ObservableProperty]
        private string _currentPage = "Home";

        [ObservableProperty]
        private bool _isProtected = true;

        [ObservableProperty]
        private string _statusText = "Protected";

        [ObservableProperty]
        private bool _isPremium;

        [ObservableProperty]
        private string _tierBadgeText = "FREE VERSION";

        [ObservableProperty]
        private string _proExpiryText = string.Empty;

        public MainViewModel(ISettingsService settingsService, IBillingService billingService)
        {
            _settingsService = settingsService;
            _billingService = billingService;
            RefreshTierStatus();

            _loc.LanguageChanged += () =>
            {
                OnPropertyChanged(nameof(StatusText));
            };
        }

        public void RefreshTierStatus()
        {
            IsPremium = _billingService.IsPremium;
            TierBadgeText = IsPremium ? "PRO EDITION" : "FREE VERSION";
            ProExpiryText = IsPremium ? $"Expires: {_billingService.FormattedExpiryDate}" : "Upgrade to Pro";
        }

        [RelayCommand]
        public void Navigate(string pageName)
        {
            CurrentPage = pageName;
        }
    }
}
