using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ViperaSecurity.Services;

namespace ViperaSecurity.ViewModels
{
    public partial class PaywallViewModel : ObservableObject
    {
        private readonly IBillingService _billingService;

        [ObservableProperty]
        private string _licenseKeyInput = string.Empty;

        [ObservableProperty]
        private bool _isPremium;

        [ObservableProperty]
        private string _priceText = "€39/year";

        [ObservableProperty]
        private string _activationMessage = string.Empty;

        [ObservableProperty]
        private string _formattedExpiryDate = string.Empty;

        [ObservableProperty]
        private string _daysRemainingText = string.Empty;

        public PaywallViewModel(IBillingService billingService)
        {
            _billingService = billingService;
            IsPremium = _billingService.IsPremium;
            PriceText = _billingService.PriceText;
            FormattedExpiryDate = _billingService.FormattedExpiryDate;
            DaysRemainingText = _billingService.DaysRemainingText;

            if (IsPremium)
            {
                LicenseKeyInput = _billingService.LicenseKey;
                ActivationMessage = $"👑 Vipera Pro Active — Expiry: {FormattedExpiryDate} ({DaysRemainingText})";
            }
        }

        [RelayCommand]
        public void SubscribeStripe()
        {
            _billingService.OpenStripeCheckout();
            ActivationMessage = "Opening Stripe Secure Checkout in your browser...";
        }

        [RelayCommand]
        public async Task ActivateKey()
        {
            if (string.IsNullOrWhiteSpace(LicenseKeyInput))
            {
                ActivationMessage = "Please enter a valid License Key or Order Token.";
                return;
            }

            if (_billingService.ActivateLicense(LicenseKeyInput))
            {
                IsPremium = true;
                FormattedExpiryDate = _billingService.FormattedExpiryDate;
                DaysRemainingText = _billingService.DaysRemainingText;
                ActivationMessage = $"Success! Vipera Pro Activated. Valid until: {FormattedExpiryDate} ({DaysRemainingText}).";
            }
            else
            {
                bool cloudSuccess = await _billingService.VerifyCloudSubscriptionAsync();
                if (cloudSuccess)
                {
                    IsPremium = true;
                    FormattedExpiryDate = _billingService.FormattedExpiryDate;
                    DaysRemainingText = _billingService.DaysRemainingText;
                    ActivationMessage = $"Success! Subscription verified. Valid until: {FormattedExpiryDate} ({DaysRemainingText}).";
                }
                else
                {
                    ActivationMessage = "Invalid License Key. Try entering 'VIPERA-PRO-2026' or check your Stripe email.";
                }
            }
        }
    }
}
