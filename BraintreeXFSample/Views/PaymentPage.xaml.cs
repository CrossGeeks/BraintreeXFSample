using BraintreeXFSample.ViewModels;
using Xamarin.Forms;

namespace BraintreeXFSample.Views
{
    public partial class PaymentPage : ContentPage
    {
        public PaymentPage()
        {
            InitializeComponent();
            BindingContext = new PaymentPageViewModel();
        }
    }
}
