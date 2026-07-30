using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViperaSecurity.Services
{
    public class StripeBillingService : IBillingService
    {
        private readonly ISettingsService _settingsService;
        private readonly SupabaseService _supabaseService;

        public const string DefaultStripeCheckoutUrl = "https://buy.stripe.com/8x2bJ0eqHgZs6JH7166wE01";
        public string PriceText => "€39/year";

        public bool IsPremium => _settingsService.Settings.IsPremium;
        public string LicenseKey => _settingsService.Settings.LicenseKey;

        public DateTime ExpiryDate
        {
            get
            {
                var expiry = _settingsService.Settings.ProSubscriptionExpiry;
                if (expiry == DateTime.MinValue && IsPremium)
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
                var date = ExpiryDate;
                return date.ToString("MMMM dd, yyyy");
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

        public StripeBillingService(ISettingsService settingsService, SupabaseService supabaseService)
        {
            _settingsService = settingsService;
            _supabaseService = supabaseService;
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
                _supabaseService.SaveAccessToken($"token_{normalized}");
                return true;
            }
            return false;
        }

        public async Task<bool> VerifyCloudSubscriptionAsync()
        {
            string? token = _supabaseService.GetCachedAccessToken();
            if (!string.IsNullOrEmpty(token))
            {
                bool isValid = await _supabaseService.VerifyProSubscriptionAsync(token);
                _settingsService.Settings.IsPremium = isValid;
                _settingsService.Save();
                return isValid;
            }
            return IsPremium;
        }

        public void OpenStripeCheckout()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DefaultStripeCheckoutUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://stripe.com",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }
    }
}
