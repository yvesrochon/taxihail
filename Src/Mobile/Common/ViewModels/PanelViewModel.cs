using System;
using Cirrious.MvvmCross.Interfaces.Commands;
using Cirrious.MvvmCross.Commands;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Booking.Mobile.Infrastructure;
using TinyMessenger;
using apcurium.MK.Booking.Mobile.Messages;
using TinyIoC;
using apcurium.MK.Booking.Api.Contract.Resources;
using Cirrious.MvvmCross.Interfaces.ServiceProvider;
using Cirrious.MvvmCross.ExtensionMethods;
using Params = System.Collections.Generic.Dictionary<string, string>;
using ServiceStack.Text;

namespace apcurium.MK.Booking.Mobile.ViewModels
{
	public class PanelViewModel : BaseViewModel,
        IMvxServiceConsumer<IAccountService>
	{
        readonly IAccountService _accountService;
		public PanelViewModel ()
		{
            _accountService = this.GetService<IAccountService>();
		}

		public void SignOut()
		{
            TinyIoCContainer.Current.Resolve<IAccountService>().SignOut();			
			InvokeOnMainThread(() => TinyIoCContainer.Current.Resolve<ITinyMessengerHub>().Publish(new LogOutRequested(this)));
		}

		public MvxRelayCommand NavigateToOrderHistory
		{
			get
			{
				return new MvxRelayCommand(() =>
				                           {
					RequestNavigate<HistoryViewModel>();
				});
			}
		}

        public IMvxCommand NavigateToUpdateProfile
        {
            get
            {
                return new MvxRelayCommand(()=>{
                    RequestSubNavigate<RideSettingsViewModel, BookingSettings>(new Params{
                        { "bookingSettings", _accountService.CurrentAccount.Settings.ToJson()  }
                    }, result => {
                        if(result!=null)
                        {
                            _accountService.UpdateSettings(result);
                        }
                    });
                });
            }
        }


	}
}

