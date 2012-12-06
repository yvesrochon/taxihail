using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using apcurium.MK.Booking.Mobile.Style;
using Android.Graphics;

namespace apcurium.MK.Booking.Mobile.Client.Helpers
{
    public static class DrawHelper
    {
        public static  int GetPixels(float dipValue)
        {
            int px = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dipValue , Application.Context.Resources.DisplayMetrics); // getDisplayMetrics());
            return px;
        }

        public static  int GetPixelsFromPt(float ptValue)
        {
            int px = (int)TypedValue.ApplyDimension(ComplexUnitType.Pt, ptValue , Application.Context.Resources.DisplayMetrics); // getDisplayMetrics());
            return px;
        }

        public static Color ConvertToColor( this ColorDefinition colorDef )
        {
            return new Color( colorDef.Red, colorDef.Green, colorDef.Blue , colorDef.Alpha );
        }
    }
}