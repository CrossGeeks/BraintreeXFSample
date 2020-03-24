using System;
using System.Threading.Tasks;
using BraintreeXFSample.Models;

namespace BraintreeXFSample.Services
{
    public interface IPayService
    {
        event EventHandler<string> OnTokenizationSuccessful;

        event EventHandler<string> OnTokenizationError;

        event EventHandler<DropUIResult> OnDropUISuccessful;

        event EventHandler<string> OnDropUIError;

        bool CanPay { get; }

        Task<bool> InitializeAsync(string clientToken);

        Task<string> TokenizeCard(string panNumber = "4111111111111111", string expirationMonth = "12", string expirationYear = "2018", string cvv = null);

        Task<string> TokenizePlatform(double totalPrice, string merchantId);

        Task<string> TokenizePayPal();

        Task<DropUIResult> ShowDropUI(double totalPrice, string merchantId, int requestCode = 1234);
    }
}
