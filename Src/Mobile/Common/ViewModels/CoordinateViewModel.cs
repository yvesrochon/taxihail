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
using apcurium.MK.Booking.Mobile.Data;

namespace apcurium.MK.Booking.Mobile.ViewModels
{
    public class CoordinateViewModel
    {
        
        public CoordinateViewModel()
        {
           
        }

        public Coordinate Coordinate { get; set; }

        public ZoomLevel Zoom{ get; set; }
        

    }
}