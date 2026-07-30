using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViperaSecurity.Services
{
    public class AutoScanScheduler
    {
        private readonly ISettingsService _settingsService;
        private readonly IFileScanner _fileScanner;
        private Timer? _timer;
        private bool _isScanning;

        public AutoScanScheduler(ISettingsService settingsService, IFileScanner fileScanner)
        {
            _settingsService = settingsService;
            _fileScanner = fileScanner;
        }

        public void Start()
        {
            // Check every 1 minute if an hour has elapsed or if scheduled scan should run
            _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
        }

        private async void OnTimerTick(object? state)
        {
            if (_isScanning) return;
            var settings = _settingsService.Settings;

            if (!settings.AutoScanEnabled || settings.AutoScanSchedule == "Disabled")
                return;

            DateTime lastScan = settings.LastScanTime;
            TimeSpan elapsed = DateTime.Now - lastScan;

            double requiredHours = settings.AutoScanSchedule switch
            {
                "Hourly (1 hr)" => 1.0,
                "Daily" => 24.0,
                "Weekly" => 168.0,
                _ => 1.0
            };

            if (lastScan == DateTime.MinValue || elapsed.TotalHours >= requiredHours)
            {
                _isScanning = true;
                try
                {
                    await _fileScanner.QuickScanAsync();
                    settings.LastScanTime = DateTime.Now;
                    settings.TotalScansCompleted++;
                    _settingsService.Save();
                }
                catch { }
                finally
                {
                    _isScanning = false;
                }
            }
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
