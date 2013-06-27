
using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Cirrious.MvvmCross.Views;
using Cirrious.MvvmCross.Binding.Touch.ExtensionMethods;
using System.Collections.Generic;
using apcurium.MK.Booking.Mobile.Client.Binding;

namespace apcurium.MK.Booking.Mobile.Client.Views.Payments
{
    public partial class ConfrimCarNumberPage :  BaseViewController<ConfirmCarNumberViewModel>
    {
        public ConfrimCarNumberPage(MvxShowViewModelRequest request) 
            : base(request)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            Container.BackgroundColor = UIColor.FromPatternImage (UIImage.FromFile ("Assets/background.png"));

            HideKeyboardButton.TouchDown += (sender, e) => {
                View.ResignFirstResponderOnSubviews();
            };

            AppButtons.FormatStandardButton((GradientButton)ConfirmButton, Resources.ConfirmButton, AppStyle.ButtonColor.Green ); 

            this.AddBindings(new Dictionary<object, string>{
                { ConfirmButton, new B("TouchDown","ConfirmTaxiNumber") }, 
                { CarNumber, new B("Text","CarNumber") }, 
            });
            
            View.ApplyAppFont ();
        }
    }
}

