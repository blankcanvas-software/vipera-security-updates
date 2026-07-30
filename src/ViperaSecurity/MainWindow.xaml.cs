using System.Windows;
using ViperaSecurity.Services;
using ViperaSecurity.ViewModels;
using ViperaSecurity.Views;

namespace ViperaSecurity
{
    public partial class MainWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private readonly IFileScanner _fileScanner;
        private readonly DomainBlocklistManager _blocklistManager;
        private readonly UrlLookupClient _urlLookupClient;
        private readonly ISystemHealthService _healthService;
        private readonly SupabaseService _supabaseService;
        private readonly IBillingService _billingService;
        private readonly UpdateService _updateService;
        private readonly AutoScanScheduler _autoScanScheduler;
        private SystemTrayService? _trayService;
        private bool _allowClose;

        private HomePage? _homePage;
        private ShieldPage? _shieldPage;
        private ScanPage? _scanPage;
        private SystemHealthPage? _healthPage;
        private SettingsPage? _settingsPage;
        private PaywallPage? _paywallPage;

        public MainWindow()
        {
            InitializeComponent();

            _settingsService = new SettingsService();
            _fileScanner = new FileScanner();
            _blocklistManager = new DomainBlocklistManager();
            _urlLookupClient = new UrlLookupClient(_blocklistManager);
            _healthService = new SystemHealthService();
            _supabaseService = new SupabaseService();
            _billingService = new StripeBillingService(_settingsService, _supabaseService);
            _updateService = new UpdateService(_supabaseService);

            _autoScanScheduler = new AutoScanScheduler(_settingsService, _fileScanner);
            _autoScanScheduler.Start();

            // Active Web Shield System Hosts Protection
            _ = System.Threading.Tasks.Task.Run(() => _blocklistManager.ApplySystemHostsProtection(_settingsService.Settings.WebProtectionEnabled));

            // Windows Startup Auto-Launch Registration
            if (_settingsService.Settings.LaunchOnWindowsStartup)
            {
                StartupManager.SetAutoStart(true);
            }

            DataContext = new MainViewModel(_settingsService, _billingService);

            InitializeSystemTray();

            NavigateToHome();

            if (_settingsService.Settings.AutoUpdateOnStartup)
            {
                _ = CheckAndApplyStartupUpdateAsync();
            }
        }

        private async System.Threading.Tasks.Task CheckAndApplyStartupUpdateAsync()
        {
            try
            {
                var info = await _updateService.CheckForUpdatesAsync();
                if (info.IsUpdateAvailable && !string.IsNullOrEmpty(info.DownloadUrl))
                {
                    await _updateService.DownloadAndApplyUpdateAsync(info.DownloadUrl, info.LatestVersion, (pct, status) => { });
                }
            }
            catch { }
        }

        private void Nav_Home(object sender, RoutedEventArgs e) => NavigateToHome();
        private void Nav_Shield(object sender, RoutedEventArgs e) => NavigateToShield();
        private void Nav_Scan(object sender, RoutedEventArgs e) => NavigateToScan();
        private void Nav_SystemHealth(object sender, RoutedEventArgs e) => NavigateToHealth();
        private void Nav_Settings(object sender, RoutedEventArgs e) => NavigateToSettings();
        private void Nav_Premium(object sender, RoutedEventArgs e) => NavigateToPremium();

        private void NavigateToHome()
        {
            _homePage ??= new HomePage
            {
                DataContext = new HomeViewModel(_settingsService, _fileScanner, page =>
                {
                    if (page == "Scan") NavigateToScan();
                    else if (page == "Shield") NavigateToShield();
                })
            };
            MainContentFrame.Content = _homePage;
        }

        private void NavigateToShield()
        {
            _shieldPage ??= new ShieldPage
            {
                DataContext = new ShieldViewModel(_settingsService, _blocklistManager, _urlLookupClient)
            };
            MainContentFrame.Content = _shieldPage;
        }

        private void NavigateToScan()
        {
            _scanPage ??= new ScanPage
            {
                DataContext = new ScanViewModel(_fileScanner, _settingsService)
            };
            MainContentFrame.Content = _scanPage;
        }

        private void NavigateToHealth()
        {
            _healthPage ??= new SystemHealthPage
            {
                DataContext = new SystemHealthViewModel(_healthService)
            };
            MainContentFrame.Content = _healthPage;
        }

        private void NavigateToSettings()
        {
            _settingsPage ??= new SettingsPage
            {
                DataContext = new SettingsViewModel(_settingsService, _updateService)
            };
            MainContentFrame.Content = _settingsPage;
        }

        private void NavigateToPremium()
        {
            (DataContext as MainViewModel)?.RefreshTierStatus();
            _paywallPage ??= new PaywallPage
            {
                DataContext = new PaywallViewModel(_billingService)
            };
            MainContentFrame.Content = _paywallPage;
        }

        private void InitializeSystemTray()
        {
            try
            {
                _trayService = new SystemTrayService();
                _trayService.Initialize(this);
                _trayService.DoubleClicked += () => RestoreFromTray();
                _trayService.RightClicked += () => RestoreFromTray();
            }
            catch { }
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_allowClose) return;

            e.Cancel = true;
            CloseConfirmationModal.Visibility = Visibility.Visible;
        }

        private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            CloseConfirmationModal.Visibility = Visibility.Collapsed;
            Hide();

            try
            {
                _trayService?.ShowNotification(
                    "Vipera Security Active",
                    "Vipera Security is running in the background to protect your device 24/7."
                );
            }
            catch { }
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            _allowClose = true;
            try
            {
                _trayService?.Remove();
            }
            catch { }
            System.Windows.Application.Current.Shutdown();
        }

        private void CancelClose_Click(object sender, RoutedEventArgs e)
        {
            CloseConfirmationModal.Visibility = Visibility.Collapsed;
        }
    }
}
