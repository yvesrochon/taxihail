using Android.App;
using Android.OS;
using SocialNetworks.Services;
using SocialNetworks.Services.MonoDroid;
using SocialNetworks.Services.OAuth;
using TinyIoC;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Booking.Mobile.Client.Activities.Book;
using apcurium.MK.Booking.Mobile.Client.Activities.Account;
using apcurium.MK.Booking.Mobile.Client.Helpers;
using System;
using apcurium.MK.Booking.Mobile.Infrastructure;
using Cirrious.MvvmCross.Android.Views;
using Cirrious.MvvmCross.Interfaces.Views;
using apcurium.MK.Booking.Mobile.ViewModels;
using Cirrious.MvvmCross.Interfaces.ViewModels;
using Cirrious.MvvmCross.Views;
using Java.Lang;

namespace apcurium.MK.Booking.Mobile.Client.Activities
{
    [Activity(Label = "@string/ApplicationName", MainLauncher = true, Theme = "@style/Theme.Splash", NoHistory = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class SplashActivity : MvxBaseSplashScreenActivity
    {

        public SplashActivity()
        {
            

        }

        private LocationService _locationService = new LocationService();

        public override Android.Views.View OnCreateView(string name, Android.Content.Context context, Android.Util.IAttributeSet attrs)
        {
            return base.OnCreateView(name, context, attrs);
        }
        protected override void OnCreate(Bundle bundle)
        {
            

            base.OnCreate(bundle);
            

            _locationService.Start();



        }


        protected override void OnResume()
        {
            base.OnResume();

            
            //DebugLogin();
            //ThreadHelper.ExecuteInThread(this, () => TinyIoCContainer.Current.Resolve<IAccountService>().RefreshCache(AppContext.Current.LoggedUser != null), false);
            //string err = "";
            //var account = TinyIoC.TinyIoCContainer.Current.Resolve<IAccountService>();
            
            

            //RunOnUiThread(() =>
            //  {
            //        Thread.Sleep( 2000 );
            //        Finish();
            //if (AppContext.Current.LoggedUser == null)
            //{
            //    //StartActivity(typeof(LoginActivity));
            //    var dispatch = TinyIoC.TinyIoCContainer.Current.Resolve<IMvxViewDispatcherProvider>().Dispatcher;
            //    dispatch.RequestNavigate(new MvxShowViewModelRequest(typeof(LoginViewModel), null, false, MvxRequestedBy.UserAction));
            //}
            //else
            //{
                
            //    var dispatch = TinyIoC.TinyIoCContainer.Current.Resolve<IMvxViewDispatcherProvider>().Dispatcher;
            //    dispatch.RequestNavigate(new MvxShowViewModelRequest(typeof(BookViewModel), null, false, MvxRequestedBy.UserAction));
            //}
            //  });
                

        }
        private void DebugLogin()
        {
            ThreadHelper.ExecuteInThread(this, () => TinyIoCContainer.Current.Resolve<IAccountService>().RefreshCache(AppContext.Current.LoggedUser != null), false);
            string err = "";
            var account = TinyIoC.TinyIoCContainer.Current.Resolve<IAccountService>().GetAccount("alex@e-nergik.com", "qqqqqq", out err);
            if (account != null)
            {
                AppContext.Current.UpdateLoggedInUser(account, false);                
                AppContext.Current.LastEmail = account.Email;
                RunOnUiThread(() =>
                {
                    Finish();

                    var dispatch = TinyIoC.TinyIoCContainer.Current.Resolve<IMvxViewDispatcherProvider>().Dispatcher;
                    dispatch.RequestNavigate(new MvxShowViewModelRequest(typeof(BookViewModel), null, false, MvxRequestedBy.UserAction));
                    //RequestNavigate<BookViewModel>();
                    //StartActivity(typeof(MainActivity));
                });
                return;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        

       
    }

}
