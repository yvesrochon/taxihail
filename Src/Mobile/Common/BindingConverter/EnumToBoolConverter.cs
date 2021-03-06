using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using apcurium.MK.Booking.Mobile.Framework.Extensions;
using Cirrious.CrossCore.Converters;

namespace apcurium.MK.Booking.Mobile.BindingConverter
{
	public class EnumToBoolConverter: MvxValueConverter
	{
		private readonly bool _inverted;

		public EnumToBoolConverter(bool inverted = false)
		{
			_inverted = inverted;
		}
		public EnumToBoolConverter()
		{
			
		}

		public override object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null || parameter == null)
			{
				return InvertResultIfNecessary (false);
			}

			var name = Enum.GetName(value.GetType(), value);

			if (name == null) 
			{
				return InvertResultIfNecessary (false);
			}

			var paramArray = parameter as IEnumerable;
			var paramString = parameter as string;

			var result = false;

			if (paramString.HasValue())
			{
				result = name.Equals(parameter.ToString());
			}
			else if (paramArray != null)
			{
				result = paramArray.Cast<object>()
					.Where(item => item != null)
					.Select(item => item.ToString())
					.Any(item => item.Equals(name));
			}

			return InvertResultIfNecessary(result);
		}

		private bool InvertResultIfNecessary(bool value)
		{
			return _inverted 
				? !value
				: value;
		}
	}
}

