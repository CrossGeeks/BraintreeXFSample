using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using BraintreeXFSample.Models;
using BraintreeXFSample.Services;
using Xamarin.Forms;

namespace BraintreeXFSample.ViewModels
{
    public class PaymentPageViewModel: INotifyPropertyChanged
    {
        public ICommand PayCommand { get; set; }
        public ICommand OnPaymentOptionSelected { get; set; }
        public PaymentOptionEnum PaymentOptionEnum { get; set; }

        public CardInfo CardInfo { get; set; } = new CardInfo();
        IPayService _payService;

        string paymentClientToken= "<<---Payment token here---->>";
        const string MerchantId = "<<---Merchant ID here---->>";
        const double AmountToPay = 200;
        
        public PaymentPageViewModel()
        {
            _payService= Xamarin.Forms.DependencyService.Get<IPayService>();
            PayCommand = new Command(async () => await CreatePayment());
            OnPaymentOptionSelected = new Command<PaymentOptionEnum>((data) => {
                PaymentOptionEnum = data;

                if (PaymentOptionEnum != PaymentOptionEnum.CreditCard)
                    PayCommand.Execute(null);
             });
            GetPaymentConfig();
        }

        async Task GetPaymentConfig()
        {
            await _payService.InitializeAsync(paymentClientToken);

        }

        async Task CreatePayment()
        {
                UserDialogs.Instance.ShowLoading("Loading");

                if (_payService.CanPay)
                {
                    try
                    {
                        _payService.OnTokenizationSuccessful += OnTokenizationSuccessful;
                        _payService.OnTokenizationError += OnTokenizationError;

                            switch (PaymentOptionEnum)
                            {
                                case PaymentOptionEnum.Platform:
                                    await _payService.TokenizePlatform(AmountToPay, MerchantId);
                                    break;
                                case PaymentOptionEnum.CreditCard:
                                     await _payService.TokenizeCard(CardInfo.CardNumber.Replace(" ", string.Empty), CardInfo.Expiry.Substring(0, 2), $"{DateTime.Now.ToString("yyyy").Substring(0, 2)}{CardInfo.Expiry.Substring(3, 2)}", CardInfo.Cvv);
                                     break;
                                default:
                                    break;
                            }
                }
                catch (Exception ex)
                    {
                        UserDialogs.Instance.HideLoading();
                        await App.Current.MainPage.DisplayAlert("Error", "Unable to process payment", "Ok");
                        System.Diagnostics.Debug.WriteLine(ex);
                    }

                }
                else
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                    {
                        UserDialogs.Instance.HideLoading();
                        await App.Current.MainPage.DisplayAlert("Error", "Payment not available", "Ok");
                    });
                }
        }

        async void OnTokenizationSuccessful(object sender, string e)
        {
            _payService.OnTokenizationSuccessful -= OnTokenizationSuccessful;
            System.Diagnostics.Debug.WriteLine($"Payment Authorized - {e}");
            UserDialogs.Instance.HideLoading();
            await App.Current.MainPage.DisplayAlert("Success", $"Payment Authorized: the token is{e}", "Ok");
           
        }

        async void OnTokenizationError(object sender, string e)
        {
            _payService.OnTokenizationError -= OnTokenizationError;
            System.Diagnostics.Debug.WriteLine(e);
            UserDialogs.Instance.HideLoading();
            await App.Current.MainPage.DisplayAlert("Error", "Unable to process payment", "Ok");
        }

        public event PropertyChangedEventHandler PropertyChanged;


    }
}
