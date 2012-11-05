using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Converters.Visibility;

namespace apcurium.MK.Booking.Mobile.BindingConverter
{
    public class BoolVisibilityInverter : MvxBaseVisibilityConverter
    {
        public override MvxVisibility ConvertToMvxVisibility(object value, object parameter, CultureInfo culture)
        {
            var visibility = !(bool)value;
            return visibility ? MvxVisibility.Visible : MvxVisibility.Collapsed;
        }
    }
}