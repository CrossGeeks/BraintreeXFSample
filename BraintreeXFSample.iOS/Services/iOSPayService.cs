using System;
using System.Threading.Tasks;
using BraintreeCard;
using BraintreeCore;
using BraintreeXFSample.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(BraintreeXFSample.iOS.Services.iOSPayService))]
namespace BraintreeXFSample.iOS.Services
{
    public class iOSPayService : IPayService
    {
        TaskCompletionSource<string> payTcs;
        public event EventHandler<string> OnTokenizationSuccessful = delegate { };
        public event EventHandler<string> OnTokenizationError = delegate { };

        bool isReady;
        BTAPIClient braintreeClient; 

        public bool CanPay
        {
            get
            {
                return isReady;
            }
        }

        public async Task<string> TokenizeCard(string panNumber = "4111111111111111", string expirationMonth = "12", string expirationYear = "2018", string cvv = null)
        {
            payTcs = new TaskCompletionSource<string>();
            if (CanPay)
            {
                var cardClient = new BTCardClient(apiClient: braintreeClient);
                var card = new BTCard(panNumber, expirationMonth, expirationYear, cvv);

                cardClient.TokenizeCard(card, (BTCardNonce tokenizedCard, Foundation.NSError error) =>
                {

                    if (error == null)
                    {
                        OnTokenizationSuccessful?.Invoke(this, tokenizedCard.Nonce);
                        payTcs.TrySetResult(tokenizedCard.Nonce);
                    }
                    else
                    {
                        OnTokenizationError?.Invoke(this, error.Description);
                        payTcs.TrySetException(new Exception(error.Description));
                    }

                });
            }
            else
            {
                OnTokenizationError?.Invoke(this, "Platform is not ready to accept payments");
                payTcs.TrySetException(new Exception("Platform is not ready to accept payments"));

            }

            return await payTcs.Task;

        }
      
        public async Task<bool> InitializeAsync(string clientToken)
        {
            var initializeTcs = new TaskCompletionSource<bool>();
            try
            {
                braintreeClient = new BTAPIClient(clientToken);
                isReady = true;
                initializeTcs.TrySetResult(isReady);
            }
            catch (Exception e)
            {
                initializeTcs.TrySetException(e);
            }
            return await initializeTcs.Task;
        }
    }
  
}
