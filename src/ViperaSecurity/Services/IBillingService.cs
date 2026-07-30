using System.Threading.Tasks;

namespace ViperaSecurity.Services
{
    public interface IBillingService
    {
        bool IsPremium { get; }
        string LicenseKey { get; }
        string PriceText { get; }
        System.DateTime ExpiryDate { get; }
        string FormattedExpiryDate { get; }
        string DaysRemainingText { get; }
        bool ActivateLicense(string key);
        void OpenStripeCheckout();
        Task<bool> VerifyCloudSubscriptionAsync();
    }
}
