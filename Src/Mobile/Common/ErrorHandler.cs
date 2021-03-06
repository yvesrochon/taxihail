using System;
using System.Net;
using System.Threading.Tasks;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Booking.Mobile.Extensions;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Booking.Mobile.ViewModels;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Extensions;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.Views;
using MK.Common.Exceptions;
using apcurium.MK.Common;

namespace apcurium.MK.Booking.Mobile
{
    public class ErrorHandler : IErrorHandler
    {
        private readonly IMvxViewDispatcher _dispatcher;
        private readonly ILocalization _localize;
        private readonly IMessageService _messageService;
        private readonly ILogger _logger;
		private readonly IConnectivityService _connectivityService;

        public static DateTime LastConnectError = DateTime.MinValue;
        public static DateTime LastGeneralError = DateTime.MinValue;
		public static DateTime LastUnauthorizedError = DateTime.MinValue;

		private static bool IsErrorMessageDisplayed;

        public ErrorHandler(IMvxViewDispatcher dispatcher, ILocalization localize, IMessageService messageService, ILogger logger, IConnectivityService connectivityService)
        {
            _dispatcher = dispatcher;
            _localize = localize;
            _messageService = messageService;
			_logger = logger;
			_connectivityService = connectivityService;
        }

        public bool HandleError (Exception ex)
        {
            var webServiceException = ex as WebServiceException;
            var webException = ex as WebException;
            var statusCode = webServiceException.SelectOrDefault(service => (int?) service.StatusCode, null) ??
                             webException.SelectOrDefault(service => (int?) service.Status, null);

            var handled = false;
            switch (statusCode)
            {
                case (int)HttpStatusCode.Unauthorized:
                {
                    HandleUnauthorizedErrorIfNeeded();
                    handled = true;
                    break;
                }
                case (int)HttpStatusCode.ServiceUnavailable:
                case (int)WebExceptionStatus.ConnectFailure:
                case (int)WebExceptionStatus.NameResolutionFailure:
                {
                    HandleConnectionError(ex);
                    handled = true;
                    break;
                }
                default:
                {
                    if (LastGeneralError.Subtract(DateTime.Now).TotalSeconds < -5)
                    {
                        LastGeneralError = DateTime.Now;

						_logger.LogError(ex, string.Empty, -1, true);
                    }
                    break;
                }
            }

			return handled;
        }

        private void HandleConnectionError(Exception ex)
        {
            if (LastConnectError.Subtract(DateTime.Now).TotalSeconds >= -5)
            {
                return;
            }

            LastConnectError = DateTime.Now;

			_connectivityService.ShowToast();

            _logger.LogError(ex);
        }

        private void HandleUnauthorizedErrorIfNeeded()
        {
            if (LastUnauthorizedError.Subtract(DateTime.Now).TotalSeconds >= -5)
            {
                return;
            }

            LastUnauthorizedError = DateTime.Now;

            _messageService.ShowMessage(
                _localize["ServiceErrorCallTitle"],
                _localize["ServiceErrorUnauthorized"],
                () => SignOutUser().FireAndForget());
        }

        private async Task SignOutUser()
        {
            _dispatcher.RequestMainThreadAction(() =>
            {
                _dispatcher.ShowViewModel(new MvxViewModelRequest(typeof (LoginViewModel), null, null, MvxRequestedBy.UserAction));
            });
            Mvx.Resolve<IAccountService>().SignOut();
            await Mvx.Resolve<IOrderWorkflowService>().PrepareForNewOrder();
        }
    }
}

