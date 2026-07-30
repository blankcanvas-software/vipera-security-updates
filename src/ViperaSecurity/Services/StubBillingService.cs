using System;
using System.Threading.Tasks;

namespace ViperaSecurity.Services
{
    public class StubBillingService : IBillingService
    {
        private readonly ISettingsService _settingsService;

        public string PriceText => "€39/year";
        public bool IsPremium => _settingsService.Settings.IsPremium;
        public string LicenseKey => _settingsService.Settings.LicenseKey;

        public DateTime ExpiryDate
        {
            get
            {
                var expiry = _settingsService.Settings.ProSubscriptionExpiry;
                if ((expiry == DateTime.MinValue || expiry <= DateTime.Now) && IsPremium)
                {
                    expiry = DateTime.Now.AddYears(1);
                    _settingsService.Settings.ProSubscriptionExpiry = expiry;
                    _settingsService.Save();
                }
                return expiry;
            }
        }

        public string FormattedExpiryDate
        {
            get
            {
                if (!IsPremium) return "Free Edition (No Expiry)";
                return ExpiryDate.ToString("MMMM dd, yyyy");
            }
        }

        public string DaysRemainingText
        {
            get
            {
                if (!IsPremium) return "Free Plan";
                var remaining = (ExpiryDate - DateTime.Now).Days;
                return remaining > 0 ? $"{remaining} days remaining" : "Expires today";
            }
        }

        public StubBillingService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public bool ActivateLicense(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            string normalized = key.Trim().ToUpperInvariant();
            if (normalized.StartsWith("VIPERA-") || normalized.Length >= 8)
            {
                _settingsService.Settings.LicenseKey = normalized;
                _settingsService.Settings.IsPremium = true;
                _settingsService.Settings.ProSubscriptionExpiry = DateTime.Now.AddYears(1);
                _settingsService.Save();
                return true;
            }
            return false;
        }

        public void OpenStripeCheckout()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://buy.stripe.com/8x2bJ0eqHgZs6JH7166wE01",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public Task<bool> VerifyCloudSubscriptionAsync()
        {
            return Task.FromResult(IsPremium);
        }
    }
}
