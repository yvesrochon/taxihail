using System;
using System.Globalization;
using apcurium.MK.Booking.Mobile.Infrastructure;
using Cirrious.MvvmCross.Converters;
using Cirrious.MvvmCross.ExtensionMethods;
using Cirrious.MvvmCross.Interfaces.ServiceProvider;

namespace apcurium.MK.Booking.Mobile.BindingConverter
{
	public class EmptyToResourceConverter: MvxBaseValueConverter, IMvxServiceConsumer<IAppResource>
	{
		public override object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!string.IsNullOrEmpty(value as string)) {
				return value;
			}
			if(string.IsNullOrEmpty((string)parameter)) {
				return null;
			}
			return this.GetService().GetString(parameter.ToString());
		}
	}
}

