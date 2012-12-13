using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using apcurium.MK.Booking.Mobile.ViewModels.Payment;
using Cirrious.MvvmCross.Binding.Touch.Views;
using Cirrious.MvvmCross.Views;
using System.Collections.Generic;
using Cirrious.MvvmCross.Binding.Touch.ExtensionMethods;

namespace apcurium.MK.Booking.Mobile.Client.Views.Payment
{
    public partial class PaymentDetails : BaseViewController<PaymentDetailsViewModel>
    {
        public PaymentDetails () 
            : base(new MvxShowViewModelRequest<PaymentDetailsViewModel>( null, true, new Cirrious.MvvmCross.Interfaces.ViewModels.MvxRequestedBy()   ) )
        {
        }
        
        public PaymentDetails (MvxShowViewModelRequest request) 
            : base(request)
        {
        }
        
        public PaymentDetails (MvxShowViewModelRequest request, string nibName, NSBundle bundle) 
            : base(request, nibName, bundle)
        {
        }
		
        public override void DidReceiveMemoryWarning ()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning ();
			
            // Release any cached data, images, etc that aren't in use.
        }
		
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            base.DismissKeyboardOnReturn(txtCreditCard, txtTipAmount);

            lblCreditCard.Text = Resources.GetValue("PaymentDetails.CreditCardLabel");
            lblTipAmount.Text = Resources.GetValue("PaymentDetails.TipAmountLabel");
			
            sgmtPercentOrValue.SelectedSegment = ViewModel.IsTipInPercent ? 0 : 1;
            sgmtPercentOrValue.ValueChanged += HandleValueChanged;

            this.AddBindings(new Dictionary<object, string> {
                { txtTipAmount, "{'Text': {'Path': 'Tip'}}" },
                { txtCreditCard, "{'Text': {'Path': 'SelectedCreditCardName'}, 'NavigateCommand': {'Path': 'NavigateToCreditCardsList'}}" },
            });

        }

        void HandleValueChanged (object sender, EventArgs e)
        {
            ViewModel.IsTipInPercent = (sgmtPercentOrValue.SelectedSegment == 0);
        }
		
        public override void ViewDidUnload ()
        {
            base.ViewDidUnload ();
			
            // Clear any references to subviews of the main view in order to
            // allow the Garbage Collector to collect them sooner.
            //
            // e.g. myOutlet.Dispose (); myOutlet = null;
			
            ReleaseDesignerOutlets ();
        }
		
        public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
        {
            // Return true for supported orientations
            return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
        }
    }
}

