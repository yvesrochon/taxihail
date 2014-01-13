using Android.App;
using Android.Widget;
using Cirrious.MvvmCross.Droid.Views;
using Cirrious.MvvmCross.ViewModels;
using apcurium.MK.Booking.Mobile.ViewModels;

namespace apcurium.MK.Booking.Mobile.Client.Activities
{
    public abstract class BaseActivity : Activity
    {
        protected abstract int ViewTitleResourceId { get; }

        protected override void OnResume()
        {
            base.OnResume();

            var txt = FindViewById<TextView>(Resource.Id.ViewTitle);
            if (txt != null)
            {
                txt.Text = GetString(ViewTitleResourceId);
            }
        }
    }

    public abstract class BaseBindingActivity<TViewModel> : MvxActivity
        where TViewModel : BaseViewModel, IMvxViewModel
    {
        protected abstract int ViewTitleResourceId { get; }

		public new TViewModel ViewModel
		{
			get
			{
				return (TViewModel)DataContext;
			}
		}

        protected override void OnResume()
        {
            base.OnResume();

            var txt = FindViewById<TextView>(Resource.Id.ViewTitle);
            if (txt != null) txt.Text = GetString(ViewTitleResourceId);
        }

        protected override void OnStart()
        {
            base.OnStart();
            ViewModel.Start();
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            ViewModel.Restart();
        }

        protected override void OnStop()
        {
            base.OnStop();
            ViewModel.Stop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ViewModel.Unload();
        }
    }

    public abstract class BaseListActivity : ListActivity
    {
        protected abstract int ViewTitleResourceId { get; }


        protected override void OnResume()
        {
            base.OnResume();

            var txt = FindViewById<TextView>(Resource.Id.ViewTitle);
            txt.Text = GetString(ViewTitleResourceId);
        }
    }
}