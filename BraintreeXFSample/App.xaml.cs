using System;
using BraintreeXFSample.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BraintreeXFSample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new PaymentPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
