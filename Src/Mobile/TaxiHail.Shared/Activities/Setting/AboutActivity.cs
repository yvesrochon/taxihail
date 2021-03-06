using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Webkit;
using apcurium.MK.Booking.Mobile.ViewModels;

namespace apcurium.MK.Booking.Mobile.Client.Activities.Setting
{
	[Activity(Label = "@string/AboutActivityName", Theme = "@style/MainTheme",
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class AboutActivity : BaseBindingActivity<AboutUsViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.View_About);
            var webView = FindViewById<WebView>(Resource.Id.aboutWebView);

            webView.LoadUrl(ViewModel.Uri);
            webView.Settings.JavaScriptEnabled = true;
        }
    }
}