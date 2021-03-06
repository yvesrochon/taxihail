// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace apcurium.MK.Booking.Mobile.Client.Views
{
    [Register ("CreditCardAddView")]
    partial class CreditCardAddView
    {
        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatButton btnCardDefault { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatButton btnDeleteCard { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatButton btnLinkPayPal { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatButton btnSaveCard { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatButton btnScanCard { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatButton btnUnlinkPayPal { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.HideableView imgAmex { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.HideableView imgDiscover { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.HideableView imgVisa { get; set; }


        [Outlet]
        UIKit.UILabel lblCardNumber { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.CountrySelector lblCountryCode { get; set; }


        [Outlet]
        UIKit.UILabel lblCvv { get; set; }


        [Outlet]
        UIKit.UILabel lblEmail { get; set; }


        [Outlet]
        UIKit.UILabel lblExpMonth { get; set; }


        [Outlet]
        UIKit.UILabel lblExpYear { get; set; }


        [Outlet]
        UIKit.UILabel lblInstructions { get; set; }


        [Outlet]
        UIKit.UILabel lblLabel { get; set; }


        [Outlet]
        UIKit.UILabel lblNameOnCard { get; set; }


        [Outlet]
        UIKit.UILabel lblPayPalLinkedInfo { get; set; }


        [Outlet]
        UIKit.UILabel lblPhoneNumber { get; set; }


        [Outlet]
        UIKit.UILabel lblStreetName { get; set; }


        [Outlet]
        UIKit.UILabel lblStreetNumber { get; set; }


        [Outlet]
        UIKit.UILabel lblTip { get; set; }


        [Outlet]
        UIKit.UILabel lblZipCode { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtCardNumber { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtCvv { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtEmail { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.ModalFlatTextField txtExpMonth { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.ModalFlatTextField txtExpYear { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtNameOnCard { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtPhoneNumber { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtStreetName { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtStreetNumber { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.ModalFlatTextField txtTip { get; set; }


        [Outlet]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtZipCode { get; set; }


        [Outlet]
        UIKit.UIView viewCreditCard { get; set; }


        [Outlet]
        UIKit.UIView viewLabel { get; set; }


        [Outlet]
        UIKit.UIView viewPayPal { get; set; }


        [Outlet]
        UIKit.UIView viewPayPalIsLinkedInfo { get; set; }


        [Outlet]
        UIKit.UIView viewTip { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        apcurium.MK.Booking.Mobile.Client.Controls.Widgets.FlatTextField txtLabel { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (btnCardDefault != null) {
                btnCardDefault.Dispose ();
                btnCardDefault = null;
            }

            if (btnDeleteCard != null) {
                btnDeleteCard.Dispose ();
                btnDeleteCard = null;
            }

            if (btnLinkPayPal != null) {
                btnLinkPayPal.Dispose ();
                btnLinkPayPal = null;
            }

            if (btnSaveCard != null) {
                btnSaveCard.Dispose ();
                btnSaveCard = null;
            }

            if (btnScanCard != null) {
                btnScanCard.Dispose ();
                btnScanCard = null;
            }

            if (btnUnlinkPayPal != null) {
                btnUnlinkPayPal.Dispose ();
                btnUnlinkPayPal = null;
            }

            if (imgAmex != null) {
                imgAmex.Dispose ();
                imgAmex = null;
            }

            if (imgDiscover != null) {
                imgDiscover.Dispose ();
                imgDiscover = null;
            }

            if (imgVisa != null) {
                imgVisa.Dispose ();
                imgVisa = null;
            }

            if (lblCardNumber != null) {
                lblCardNumber.Dispose ();
                lblCardNumber = null;
            }

            if (lblCountryCode != null) {
                lblCountryCode.Dispose ();
                lblCountryCode = null;
            }

            if (lblCvv != null) {
                lblCvv.Dispose ();
                lblCvv = null;
            }

            if (lblEmail != null) {
                lblEmail.Dispose ();
                lblEmail = null;
            }

            if (lblExpMonth != null) {
                lblExpMonth.Dispose ();
                lblExpMonth = null;
            }

            if (lblExpYear != null) {
                lblExpYear.Dispose ();
                lblExpYear = null;
            }

            if (lblInstructions != null) {
                lblInstructions.Dispose ();
                lblInstructions = null;
            }

            if (lblLabel != null) {
                lblLabel.Dispose ();
                lblLabel = null;
            }

            if (lblNameOnCard != null) {
                lblNameOnCard.Dispose ();
                lblNameOnCard = null;
            }

            if (lblPayPalLinkedInfo != null) {
                lblPayPalLinkedInfo.Dispose ();
                lblPayPalLinkedInfo = null;
            }

            if (lblPhoneNumber != null) {
                lblPhoneNumber.Dispose ();
                lblPhoneNumber = null;
            }

            if (lblStreetName != null) {
                lblStreetName.Dispose ();
                lblStreetName = null;
            }

            if (lblStreetNumber != null) {
                lblStreetNumber.Dispose ();
                lblStreetNumber = null;
            }

            if (lblTip != null) {
                lblTip.Dispose ();
                lblTip = null;
            }

            if (lblZipCode != null) {
                lblZipCode.Dispose ();
                lblZipCode = null;
            }

            if (txtCardNumber != null) {
                txtCardNumber.Dispose ();
                txtCardNumber = null;
            }

            if (txtCvv != null) {
                txtCvv.Dispose ();
                txtCvv = null;
            }

            if (txtEmail != null) {
                txtEmail.Dispose ();
                txtEmail = null;
            }

            if (txtExpMonth != null) {
                txtExpMonth.Dispose ();
                txtExpMonth = null;
            }

            if (txtExpYear != null) {
                txtExpYear.Dispose ();
                txtExpYear = null;
            }

            if (txtLabel != null) {
                txtLabel.Dispose ();
                txtLabel = null;
            }

            if (txtNameOnCard != null) {
                txtNameOnCard.Dispose ();
                txtNameOnCard = null;
            }

            if (txtPhoneNumber != null) {
                txtPhoneNumber.Dispose ();
                txtPhoneNumber = null;
            }

            if (txtStreetName != null) {
                txtStreetName.Dispose ();
                txtStreetName = null;
            }

            if (txtStreetNumber != null) {
                txtStreetNumber.Dispose ();
                txtStreetNumber = null;
            }

            if (txtTip != null) {
                txtTip.Dispose ();
                txtTip = null;
            }

            if (txtZipCode != null) {
                txtZipCode.Dispose ();
                txtZipCode = null;
            }

            if (viewCreditCard != null) {
                viewCreditCard.Dispose ();
                viewCreditCard = null;
            }

            if (viewLabel != null) {
                viewLabel.Dispose ();
                viewLabel = null;
            }

            if (viewPayPal != null) {
                viewPayPal.Dispose ();
                viewPayPal = null;
            }

            if (viewPayPalIsLinkedInfo != null) {
                viewPayPalIsLinkedInfo.Dispose ();
                viewPayPalIsLinkedInfo = null;
            }

            if (viewTip != null) {
                viewTip.Dispose ();
                viewTip = null;
            }
        }
    }
}