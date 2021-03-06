using System;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Common.Diagnostic;
using Cirrious.CrossCore;
using TinyIoC;

namespace apcurium.MK.Booking.Mobile.Extensions
{
	public static class UseServiceClientExtensions
	{
		public static string UseServiceClient<T>(this IUseServiceClient service, Action<T> action, Action<Exception> errorHandler = null) where T : class
		{
			return UseServiceClient(service, null, action, errorHandler);
		}

		public static string UseServiceClient<T>(this IUseServiceClient service, string name, Action<T> action, Action<Exception> errorHandler = null ) where T : class
		{
			var logger = Mvx.Resolve<ILogger>();
			try
			{
				using(logger.StartStopwatch("UseServiceClient : " + typeof(T)))
				{
				    var client = name == null 
                        ? TinyIoCContainer.Current.Resolve<T>() 
                        : TinyIoCContainer.Current.Resolve<T>(name);

				    action(client);
				}
			    return "";
			}
			catch (Exception ex)
			{                    
				logger.LogError(ex);
				if (errorHandler == null)
				{
					Mvx.Resolve<IErrorHandler>().HandleError(ex);
				}
				else
				{
					errorHandler(ex);
				}
				return ex.Message;
			}
		}
	}
}

