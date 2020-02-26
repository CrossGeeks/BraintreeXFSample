using System;
using BraintreeCore;
using Foundation;
using UIKit;


namespace BraintreeXFSample.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        public const string PayPalUrlScheme = "com.crossgeeks.sample.payments";
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
           
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            BTAppSwitch.SetReturnURLScheme(PayPalUrlScheme);

            return base.FinishedLaunching(app, options);
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (url.Scheme.Equals(PayPalUrlScheme, StringComparison.InvariantCultureIgnoreCase))
            {
                return BTAppSwitch.HandleOpenURL(url, options: options);
            }

            return false;
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            if (url.Scheme.Equals(PayPalUrlScheme, StringComparison.InvariantCultureIgnoreCase))
            {
                return BTAppSwitch.HandleOpenURL2(url, sourceApplication: sourceApplication);
            }
            return false;
        }
    }
}
