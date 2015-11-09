using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Booking.Mobile.Extensions;

namespace apcurium.MK.Booking.Mobile.ViewModels.Callbox
{
    public class CallboxLoginViewModel : BaseViewModel
    {
        private readonly IAccountService _accountService;
	    private readonly IBookingService _bookingService;

        public CallboxLoginViewModel(IAccountService accountService, IBookingService bookingService)
        {
	        _accountService = accountService;
	        _bookingService = bookingService;
        }

	    public override void Start()
        {
#if DEBUG
            Email = "john@taxihail.com";
            Password = "password";
#endif
        }

        private string _email;
        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
				RaisePropertyChanged();
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
				RaisePropertyChanged();
            }
        }

        public ICommand SignInCommand
        {
            get
            {
                return this.GetCommand(() =>
                {
					_accountService.ClearCache();

					return SignIn();
                });
            }
        }

        private async Task SignIn()
        {
            try
            {
                Logger.LogMessage("SignIn with server {0}", Settings.ServiceUrl);
				this.Services().Message.ShowProgress(true);

                var account = await _accountService.SignIn(Email, Password);

                if (account != null)
                {
                    Password = string.Empty;

                    var activeOrders = await _accountService.GetActiveOrdersStatus();

                    if (activeOrders.Any(c => _bookingService.IsCallboxStatusActive(c.IBSStatusId)))
                    {
						ShowViewModelAndRemoveFromHistory<CallboxOrderListViewModel>();
                    }
                    else
                    {
                        ShowViewModelAndRemoveFromHistory<CallboxCallTaxiViewModel>();
                    }
                }
            }
            catch (AuthException e)
            {
                var localize = this.Services().Localize;
                switch (e.Failure)
                {
                    case AuthFailure.InvalidServiceUrl:
                    case AuthFailure.NetworkError:
                    {
                        var title = localize["NoConnectionTitle"];
                        var msg = localize["NoConnectionMessage"];
                        this.Services().Message.ShowMessage(title, msg);
                    }
                    break;
                    case AuthFailure.InvalidUsernameOrPassword:
                    {
                        var title = localize["InvalidLoginMessageTitle"];
                        var message = localize["InvalidLoginMessage"];
                        this.Services().Message.ShowMessage(title, message);
                    }
                    break;
                    case AuthFailure.AccountDisabled:
                    {
                        var title = this.Services().Localize["InvalidLoginMessageTitle"];
                        var message = localize["AccountDisabled_NoCall"];
                        this.Services().Message.ShowMessage(title, message);
                    }
                    break;
                    case AuthFailure.AccountNotActivated:
                    {
                        var title = localize["InvalidLoginMessageTitle"];
                        var message = localize["AccountNotActivated"];

                        this.Services().Message.ShowMessage(title, message);
                    }
                    break;
                }
            }
            finally
            {
				this.Services().Message.ShowProgress(false);
            }
        }
    }
}