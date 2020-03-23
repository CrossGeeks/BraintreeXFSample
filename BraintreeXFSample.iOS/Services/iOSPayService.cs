using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acr.UserDialogs;
using BraintreeApplePay;
using BraintreeCard;
using BraintreeCore;
using BraintreeDropIn;
using BraintreePayPal;
using BraintreeUIKit;
using BraintreeXFSample.Models;
using BraintreeXFSample.Services;
using Foundation;
using PassKit;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(BraintreeXFSample.iOS.Services.iOSPayService))]
namespace BraintreeXFSample.iOS.Services
{
    public class iOSPayService : PKPaymentAuthorizationViewControllerDelegate, IPayService
    {
        bool isDropUI = false;
        string _clientToken;
        TaskCompletionSource<string> payTcs;
        TaskCompletionSource<DropUIResult> dropUiPayTcs;
        PKPaymentAuthorizationViewController pKPaymentAuthorizationViewController;

        public event EventHandler<DropUIResult> OnDropUISuccessful = delegate { };
        public event EventHandler<string> OnDropUIError = delegate { };

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

        public async Task<bool> InitializeAsync(string clientToken)
        {
           
            var initializeTcs = new TaskCompletionSource<bool>();
            try
            {
                _clientToken = clientToken;
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


        public async Task<string> TokenizePlatform(double totalPrice, string merchantId)
        {
            payTcs = new TaskCompletionSource<string>();
            if (isReady)
            {
                var applePayClient = new BTApplePayClient(braintreeClient);
                applePayClient.PaymentRequest((request, error) =>
                {

                    if (error == null)
                    {
                        RequestPaymentAuthorization(request, new Dictionary<string, double>{
                               { "My App",totalPrice}
                         }, merchantId);
                    }
                    else
                    {
                        if (!isDropUI)
                        {
                            OnTokenizationError?.Invoke(this, "Error: Couldn't create payment request.");
                        }
                        
                        payTcs.TrySetException(new Exception("Error: Couldn't create payment request."));
                        
                    }
                });

            }
            else
            {
                if (!isDropUI)
                {
                    OnTokenizationError?.Invoke(this, "Platform is not ready to accept payments");
                }
                payTcs.TrySetException(new Exception("Platform is not ready to accept payments"));
                
            }

            return await payTcs.Task;
        }

        void RequestPaymentAuthorization(PKPaymentRequest paymentRequest, IDictionary<string, double> summaryItems, string merchantId)
        {
            UserDialogs.Instance.ShowLoading("Loading");
       
            paymentRequest.MerchantIdentifier = merchantId;
            paymentRequest.MerchantCapabilities = PKMerchantCapability.ThreeDS;
            paymentRequest.CountryCode = "US";
            paymentRequest.CurrencyCode = "USD";

            if (summaryItems != null)
            {
                paymentRequest.PaymentSummaryItems = summaryItems.Select(i => new PKPaymentSummaryItem()
                {
                    Label = i.Key,
                    Amount = new NSDecimalNumber(i.Value)
                }).ToArray();
            }

            var window = UIApplication.SharedApplication.KeyWindow;
            var _viewController = window.RootViewController;
            while (_viewController.PresentedViewController != null)
                _viewController = _viewController.PresentedViewController;


            pKPaymentAuthorizationViewController = new PKPaymentAuthorizationViewController(paymentRequest);
            UserDialogs.Instance.HideLoading();
            if (pKPaymentAuthorizationViewController != null)
            {
                pKPaymentAuthorizationViewController.Delegate = this;
                _viewController?.PresentViewController(pKPaymentAuthorizationViewController, true, null);
            }
            else
            {
                if (!isDropUI)
                {
                    OnTokenizationError?.Invoke(this, "Error: Payment request is invalid.");
                }

                payTcs?.SetException(new Exception("Error: Payment request is invalid."));

            }
        }


        public override void DidAuthorizePayment(PKPaymentAuthorizationViewController controller, PKPayment payment, Action<PKPaymentAuthorizationStatus> completion)
        {
            var applePayClient = new BTApplePayClient(braintreeClient);
            applePayClient.TokenizeApplePayPayment(payment, (tokenizedApplePayPayment, error) =>
            {
                if (error == null)
                {
                    if (string.IsNullOrEmpty(tokenizedApplePayPayment.Nonce))
                    {
                        payTcs?.SetCanceled();

                    }
                    else
                    {
                        if (!isDropUI)
                        {
                            OnTokenizationSuccessful?.Invoke(this, tokenizedApplePayPayment.Nonce);
                        }
                        
                        payTcs?.TrySetResult(tokenizedApplePayPayment.Nonce);
                    }

                    completion(PKPaymentAuthorizationStatus.Success);
                }
                else
                {
                    if (!isDropUI)
                    {
                        OnTokenizationError?.Invoke(this, "Error - Payment tokenization failed");
                    }

                    payTcs?.TrySetException(new Exception("Error - Payment tokenization failed"));

                    completion(PKPaymentAuthorizationStatus.Failure);
                }
            });
        }

        public override void PaymentAuthorizationViewControllerDidFinish(PKPaymentAuthorizationViewController controller)
        {
            controller.DismissViewController(true, null);
        }

        public override void WillAuthorizePayment(PKPaymentAuthorizationViewController controller)
        {

        }



        public async Task<string> TokenizePayPal()
        {
            payTcs = new TaskCompletionSource<string>();
            if (CanPay)
            {
                var payPalDriver = new BTPayPalDriver(braintreeClient);
                payPalDriver.ViewControllerPresentingDelegate = new BTViewControllerPresenter();
                payPalDriver.AppSwitchDelegate = new BTSwitchDelegate();
                payPalDriver.AuthorizeAccountWithCompletion((BTPayPalAccountNonce payPalAccountNonce, NSError error) =>
                {
                    if (error == null)
                    {
                        if (payPalAccountNonce == null || string.IsNullOrEmpty(payPalAccountNonce.Nonce))
                        {
                            payTcs.SetCanceled();
                        }
                        else
                        {
                            OnTokenizationSuccessful?.Invoke(this, payPalAccountNonce.Nonce);
                            payTcs.TrySetResult(payPalAccountNonce.Nonce);
                        }

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

        public async Task<DropUIResult> ShowDropUI(double totalPrice, string merchantId, int resultCode = 1234)
        {
            dropUiPayTcs = new TaskCompletionSource<DropUIResult>();
            if (CanPay)
            {
                BTDropInRequest request = new BTDropInRequest();
                request.Amount = $"{totalPrice}";
                BTDropInController bTDropInController = new BTDropInController(_clientToken, request, async(controller, result, error) =>
                   {
                       if (error == null)
                       {
                           if (result.Cancelled)
                           {
                               dropUiPayTcs.SetCanceled();
                           }
                           else if(result.PaymentOptionType == BTUIKPaymentOptionType.ApplePay)
                           {
                            
                               try
                               {
                                   isDropUI = true;
                                   var nonce= await TokenizePlatform(totalPrice, merchantId);

                                   var dropResult = new DropUIResult()
                                   {
                                       Nonce = nonce ?? string.Empty,
                                       Type = $"{BTUIKPaymentOptionType.ApplePay}"
                                   };
                                   OnDropUISuccessful?.Invoke(this, dropResult);
                                   dropUiPayTcs.TrySetResult(dropResult);
                               }
                               catch(TaskCanceledException)
                               {
                                   dropUiPayTcs.SetCanceled();
                               }
                               catch (Exception exception)
                               {
                                   OnDropUIError?.Invoke(this, exception.Message);
                                   dropUiPayTcs.TrySetException(exception);
                               }
                               finally
                               {
                                  pKPaymentAuthorizationViewController?.DismissViewController(true, null);
                                  isDropUI = false;
                               }

                             
                           }
                           else
                           {
                               var dropResult = new DropUIResult()
                               {
                                   Nonce = result.PaymentMethod?.Nonce ?? string.Empty,
                                   Type = $"{result.PaymentOptionType}"
                               };
                               OnDropUISuccessful?.Invoke(this, dropResult);
                               dropUiPayTcs.TrySetResult(dropResult);
                           }

                       }
                       else
                       {
                           OnDropUIError?.Invoke(this, error.Description);
                           dropUiPayTcs.TrySetException(new Exception(error.Description));
                       }

                      
                       controller.DismissViewController(true, null);
                   });

                var window = UIApplication.SharedApplication.KeyWindow;
                var _viewController = window.RootViewController;
                while (_viewController.PresentedViewController != null)
                    _viewController = _viewController.PresentedViewController;

                _viewController?.PresentViewController(bTDropInController, true, null);
            }
            else
            {
                OnDropUIError?.Invoke(this, "Platform is not ready to accept payments");
                dropUiPayTcs.TrySetException(new Exception("Platform is not ready to accept payments"));

            }
            return await dropUiPayTcs.Task;
        }
    }

    public class BTSwitchDelegate : BTAppSwitchDelegate
    {
        public override void AppSwitcher(NSObject appSwitcher, BTAppSwitchTarget target)
        {

        }

        public override void AppSwitcherWillPerformAppSwitch(NSObject appSwitcher)
        {
            UserDialogs.Instance.ShowLoading("Loading");
            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, HideLoading);
        }

        void HideLoading(NSNotification obj)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(UIApplication.DidBecomeActiveNotification);
            UserDialogs.Instance.HideLoading();
        }


        public override void AppSwitcherWillProcessPaymentInfo(NSObject appSwitcher)
        {
            HideLoading(null);
        }
    }


    public class BTViewControllerPresenter : BTViewControllerPresentingDelegate
    {
        public override void RequestsDismissalOfViewController(NSObject driver, UIViewController viewController)
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            var _viewController = window.RootViewController;
            while (_viewController.PresentedViewController != null)
                _viewController = _viewController.PresentedViewController;

            _viewController?.PresentViewController(viewController, true, null);
        }

        public override void RequestsPresentationOfViewController(NSObject driver, UIViewController viewController)
        {
            viewController.DismissViewController(true, null);
        }
    }

}
