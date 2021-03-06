using apcurium.MK.Booking.Mobile.Client.Localization;
using apcurium.MK.Booking.Mobile.ViewModels.Payment;
using UIKit;
using Cirrious.MvvmCross.Binding.BindingContext;
using Card.IO;
using System;
using Foundation;
using Cirrious.CrossCore;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Common.Configuration.Impl;
using PaypalSdkTouch.Unified;
using apcurium.MK.Booking.Mobile.Client.Style;
using apcurium.MK.Booking.Mobile.Client.Diagnostics;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Booking.Mobile.Client.Controls.Widgets;
using apcurium.MK.Booking.Mobile.Client.Extensions.Helpers;
using apcurium.MK.Booking.Mobile.Extensions;
using apcurium.MK.Common;
using apcurium.MK.Common.Extensions;

namespace apcurium.MK.Booking.Mobile.Client.Views
{
	public partial class CreditCardAddView : BaseViewController<CreditCardAddViewModel>
    {
        private PayPalClientSettings _payPalSettings;
        private CardIOPaymentViewController _cardScanner;
        private CardScannerDelegate _cardScannerDelegate;
        private PayPalCustomFuturePaymentViewController _payPalPayment;
        private PayPalDelegate _payPalPaymentDelegate;

        private bool PayPalIsEnabled
        {
            get { return _payPalSettings.IsEnabled; }
        }

        public CreditCardAddView () : base("CreditCardAddView", null)
        {
        }
		
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (NavigationController != null)
            {
                NavigationController.NavigationBar.Hidden = false;
                ChangeThemeOfBarStyle();
            }

			NavigationItem.HidesBackButton = ViewModel.IsMandatory;
            NavigationItem.Title = Localize.GetValue ("View_CreditCard");

            ChangeRightBarButtonFontToBold();

            Utilities.Preload();
        }

        public override async void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.BackgroundColor = UIColor.FromRGB (242, 242, 242);

            var paymentSettings = await Mvx.Resolve<IPaymentService>().GetPaymentSettings();
            _payPalSettings = paymentSettings.PayPalClientSettings;

            lblInstructions.Text = Localize.GetValue("CreditCardInstructions");

            if (!ViewModel.CanChooseTip)
            {
                viewTip.RemoveFromSuperview();
            }
            else
            {
                ConfigureTipSection();
            }

            if (!ViewModel.CanChooseLabel)
            {
                viewLabel.RemoveFromSuperview();
            }
            else
            {
                ConfigureLabelSection();
            }

            if (!ViewModel.ShowInstructions)
            {
                lblInstructions.RemoveFromSuperview();
            }

            if (!ViewModel.IsPayPalOnly)
            {
                ConfigureCreditCardSection();
            }
            else
            {
                viewCreditCard.RemoveFromSuperview();
            }

            if (PayPalIsEnabled)
            {
                ConfigurePayPalSection();
            }
            else
            {
                viewPayPal.RemoveFromSuperview();
            }

			if (ViewModel.PaymentSettings.EnableAddressVerification)
			{
				lblStreetName.Text = Localize.GetValue("CreditCardAdd_StreetNameLabel");
 				lblStreetNumber.Text = Localize.GetValue("CreditCardAdd_StreetNumberLabel");

 				txtStreetName.Placeholder = Localize.GetValue("CreditCardAdd_StreetNameLabel");
				txtStreetName.AccessibilityLabel = txtStreetName.Placeholder;
				txtStreetNumber.Placeholder = Localize.GetValue("CreditCardAdd_StreetNumberLabel");
				txtStreetNumber.AccessibilityLabel = txtStreetNumber.Placeholder;
                lblEmail.Text = Localize.GetValue("EmailLabel");
                lblPhoneNumber.Text = Localize.GetValue("PassengerPhoneLabel");

                txtEmail.Placeholder = Localize.GetValue("RideSettingsEmailTitle");
                txtEmail.AccessibilityLabel = txtEmail.Placeholder;
                txtPhoneNumber.Placeholder = Localize.GetValue("RideSettingsPhone");
                txtPhoneNumber.AccessibilityLabel = txtPhoneNumber.Placeholder;

                lblCountryCode.Configure(NavigationController, ViewModel.SelectedCountryCode, countryCode => ViewModel.SelectedCountryCode = countryCode);
                lblCountryCode.Font = UIFont.FromName(FontName.HelveticaNeueLight, 38 / 2);
                lblCountryCode.TintColor = UIColor.Black;
                lblCountryCode.TextColor = UIColor.FromRGB(44, 44, 44);
                lblCountryCode.TextAlignment = UITextAlignment.Center;
                lblCountryCode.AdjustsFontSizeToFitWidth = true;
                lblCountryCode.BackgroundColor = UIColor.White;
            }
			else
			{
				lblStreetName.RemoveFromSuperview();
				lblStreetNumber.RemoveFromSuperview();
                lblEmail.RemoveFromSuperview();
                lblPhoneNumber.RemoveFromSuperview();
            }

			var set = this.CreateBindingSet<CreditCardAddView, CreditCardAddViewModel>();

            set.Bind(btnSaveCard)
                .For("Title")
                .To(vm => vm.CreditCardSaveButtonDisplay);

			set.Bind(txtCvv)
                .For("HiddenEx")
				.To(vm => vm.IsAddingNewCard)
				.WithConversion("BoolInverter");

			set.Bind(lblCvv)
				.For("HiddenEx")
				.To(vm => vm.IsAddingNewCard)
				.WithConversion("BoolInverter");

            set.Bind(btnSaveCard)
                .For("TouchUpInside")
				.To(vm => vm.SaveCreditCardCommand);

			set.Bind(btnDeleteCard)
				.For("TouchUpInside")
				.To(vm => vm.DeleteCreditCardCommand);
            
			set.Bind(btnDeleteCard)
                .For("HiddenEx")
                .To(vm => vm.CanDeleteCreditCard)
				.WithConversion("BoolInverter");
            
            set.Bind(btnCardDefault)
                .For("TouchUpInside")
                .To(vm => vm.SetAsDefault);
            
            set.Bind(btnCardDefault)
                .For("HiddenEx")
                .To(vm => vm.CanSetCreditCardAsDefault)
                .WithConversion("BoolInverter");

            set.Bind(btnScanCard)
                .For("HiddenEx")
                .To(vm => vm.CanScanCreditCard)
                .WithConversion("BoolInverter");

            set.Bind(txtNameOnCard)
				.For(v => v.Text)
				.To(vm => vm.Data.NameOnCard);

			set.Bind(txtNameOnCard)
				.For(v => v.Enabled)
				.To(vm => vm.IsAddingNewCard);

            set.Bind(txtZipCode)
                .For(v => v.Text)
                .To(vm => vm.Data.ZipCode);

			set.Bind(txtZipCode)
				.For(v => v.Enabled)
				.To(vm => vm.IsAddingNewCard);

			set.Bind(txtCardNumber)
				.For(v => v.Text)
				.To(vm => vm.CreditCardNumber);
            
			set.Bind(txtCardNumber)
				.For(v => v.ImageLeftSource)
				.To(vm => vm.CreditCardImagePath);

			set.Bind(txtCardNumber)
				.For(v => v.Enabled)
				.To(vm => vm.IsAddingNewCard);

            set.Bind(txtExpMonth)
                .For(v => v.Text)
				.To(vm => vm.ExpirationMonthDisplay);

			set.Bind(txtExpMonth)
				.For(v => v.Enabled)
				.To(vm => vm.IsAddingNewCard);

			set.Bind(txtExpMonth)
				.For(v => v.HasRightArrow)
				.To(vm => vm.IsAddingNewCard);

            set.Bind(txtExpYear)
                .For(v => v.Text)
				.To(vm => vm.ExpirationYearDisplay);

			set.Bind(txtExpYear)
				.For(v => v.Enabled)
				.To(vm => vm.IsAddingNewCard);

			set.Bind(txtExpYear)
				.For(v => v.HasRightArrow)
				.To(vm => vm.IsAddingNewCard);

            set.Bind(txtCvv)
				.For(v => v.Text)
				.To(vm => vm.Data.CCV);

			set.Bind(txtCvv)
				.For(v => v.Enabled)
				.To(vm => vm.IsAddingNewCard);

			set.Bind(txtEmail)
			   .For(v => v.Enabled)
			   .To(vm => vm.IsAddingNewCard);

			set.Bind(txtPhoneNumber)
			   .For(v => v.Enabled)
			   .To(vm => vm.IsAddingNewCard);

			set.Bind(lblCountryCode)
			   .For(v => v.Enabled)
			   .To(vm => vm.IsAddingNewCard);

			set.Bind(txtStreetName)
			   .For(v => v.Enabled)
			   .To(vm => vm.IsAddingNewCard);

			set.Bind(txtStreetNumber)
			   .For(v => v.Enabled)
			   .To(vm => vm.IsAddingNewCard);

            set.Bind(btnLinkPayPal)
                .For(v => v.Hidden)
                .To(vm => vm.CanLinkPayPalAccount)
                .WithConversion("BoolInverter");

            set.Bind(btnUnlinkPayPal)
                .For(v => v.Hidden)
                .To(vm => vm.CanUnlinkPayPalAccount)
                .WithConversion("BoolInverter");

            set.Bind(viewPayPalIsLinkedInfo)
                .For(v => v.Hidden)
                .To(vm => vm.ShowLinkedPayPalInfo)
                .WithConversion("BoolInverter");

            set.Bind(txtTip)
                .For(v => v.Text)
                .To(vm => vm.PaymentPreferences.TipAmount);

            set.Bind(imgVisa)
                .For(v => v.HiddenWithConstraints)
                .To(vm => vm.PaymentSettings.DisableVisaMastercard);

            set.Bind(imgAmex)
                .For(v => v.HiddenWithConstraints)
                .To(vm => vm.PaymentSettings.DisableAMEX);

			set.Bind(lblCountryCode)
			   .For(v => v.SelectedCountryCode)
			   .To(vm => vm.SelectedCountryCode);

			set.Bind(txtLabel)
			   .For(v => v.Text)
			   .To(vm => vm.Data.Label);

            set.Bind(imgDiscover)
                .For(v => v.HiddenWithConstraints)
                .To(vm => vm.PaymentSettings.DisableDiscover);

			set.Bind(txtStreetName)
			   .For("HiddenEx")
			   .To(vm => vm.PaymentSettings.EnableAddressVerification)
			   .WithConversion("BoolInverter");

			set.Bind(txtStreetNumber)
			   .For("HiddenEx")
			   .To(vm => vm.PaymentSettings.EnableAddressVerification)
			   .WithConversion("BoolInverter");

			set.Bind(txtStreetName)
			   .To(vm => vm.Data.StreetName);

			set.Bind(txtStreetNumber)
			   .To(vm => vm.Data.StreetNumber);

			set.Bind(txtEmail)
			   .For("HiddenEx")
			   .To(vm => vm.PaymentSettings.EnableAddressVerification)
			   .WithConversion("BoolInverter");

			set.Bind(txtPhoneNumber)
			   .For("HiddenEx")
			   .To(vm => vm.PaymentSettings.EnableAddressVerification)
			   .WithConversion("BoolInverter");

			set.Bind(txtEmail)
			   .To(vm => vm.Data.Email);

			set.Bind(txtPhoneNumber)
			   .To(vm => vm.Data.Phone);
			
			set.Apply ();   

            txtNameOnCard.ShouldReturn += GoToNext;
        }

        private void ConfigureTipSection()
        {
            lblTip.Text = Localize.GetValue("PaymentDetails.TipAmountLabel");

            txtTip.Placeholder = Localize.GetValue("PaymentDetails.TipAmountLabel");
            txtTip.AccessibilityLabel = txtTip.Placeholder;

            txtTip.Configure(Localize.GetValue("PaymentDetails.TipAmountLabel"), () => ViewModel.PaymentPreferences.Tips, () => ViewModel.PaymentPreferences.Tip, x => ViewModel.PaymentPreferences.Tip = (int)x.Id, true);
            txtTip.TextAlignment = UITextAlignment.Right;
        }

        private void ConfigureLabelSection()
        {
            lblLabel.Text = Localize.GetValue("PaymentDetails.LabelName");
			txtLabel.MaxLength = 40;
        }

        private void ConfigureCreditCardSection()
        {
            FlatButtonStyle.Silver.ApplyTo(btnScanCard);
            btnScanCard.SetTitle(Localize.GetValue("ScanCreditCard"), UIControlState.Normal);
            btnScanCard.TouchUpInside += (sender, e) => ScanCard();

            FlatButtonStyle.Silver.ApplyTo(btnCardDefault);
            // Configure CreditCard section
            FlatButtonStyle.Green.ApplyTo(btnSaveCard);
            FlatButtonStyle.Red.ApplyTo (btnDeleteCard);
            btnDeleteCard.SetTitle(Localize.GetValue("DeleteCreditCardTitle"), UIControlState.Normal);

            lblNameOnCard.Text = Localize.GetValue("CreditCardName");
            lblCardNumber.Text = Localize.GetValue("CreditCardNumber");
            lblExpMonth.Text = Localize.GetValue("CreditCardExpMonth");
            lblExpYear.Text = Localize.GetValue("CreditCardExpYear");
            lblCvv.Text = Localize.GetValue("CreditCardCCV");
            lblZipCode.Text = Localize.GetValue("CreditCardZipCode");

            txtNameOnCard.AccessibilityLabel = Localize.GetValue("CreditCardName");
            txtNameOnCard.Placeholder = txtNameOnCard.AccessibilityLabel;

            txtZipCode.AccessibilityLabel = Localize.GetValue("CreditCardZipCode");
            txtZipCode.Placeholder = txtZipCode.AccessibilityLabel;

            txtCardNumber.AccessibilityLabel = Localize.GetValue("CreditCardNumber");
            txtCardNumber.Placeholder = txtCardNumber.AccessibilityLabel;

            txtExpMonth.AccessibilityLabel = Localize.GetValue("CreditCardExpMonth");
            txtExpMonth.Placeholder = txtExpMonth.AccessibilityLabel;

            txtExpYear.AccessibilityLabel = Localize.GetValue("CreditCardExpYear");
            txtExpYear.Placeholder = txtExpYear.AccessibilityLabel;

            txtCvv.AccessibilityLabel = Localize.GetValue("CreditCardCCV");
            txtCvv.Placeholder = txtCvv.AccessibilityLabel;

            txtCardNumber.ClearsOnBeginEditing = true;
            txtCardNumber.ShowCloseButtonOnKeyboard();
            txtCvv.ShowCloseButtonOnKeyboard();
            txtZipCode.ShowCloseButtonOnKeyboard();

            ViewModel.CreditCardCompanies[0].Image = "visa.png";
            ViewModel.CreditCardCompanies[1].Image = "mastercard.png";
            ViewModel.CreditCardCompanies[2].Image = "amex.png";
            ViewModel.CreditCardCompanies[3].Image = "visa_electron.png";
            ViewModel.CreditCardCompanies[4].Image = "discover.png";
            ViewModel.CreditCardCompanies[5].Image = "credit_card_generic.png";

            txtExpMonth.Configure(Localize.GetValue("CreditCardExpMonth"), () => ViewModel.ExpirationMonths.ToArray(), () => ViewModel.ExpirationMonth, x => ViewModel.ExpirationMonth = x.Id);
            txtExpYear.Configure(Localize.GetValue("CreditCardExpYear"), () => ViewModel.ExpirationYears.ToArray(), () => ViewModel.ExpirationYear, x => ViewModel.ExpirationYear = x.Id);
        }
            
        private void ConfigurePayPalSection()
        {
            Mvx.Resolve<IPayPalConfigurationService>().InitializeService(_payPalSettings);
            
            lblPayPalLinkedInfo.Text = Localize.GetValue("PayPalLinkedInfo");

            FlatButtonStyle.Silver.ApplyTo(btnLinkPayPal);
			btnLinkPayPal.SetLeftImage("paypal_icon.png");
            btnLinkPayPal.SetTitle(Localize.GetValue("LinkPayPal"), UIControlState.Normal);
            btnLinkPayPal.TouchUpInside += (sender, e) => PayPalFlow();

            FlatButtonStyle.Silver.ApplyTo(btnUnlinkPayPal);
			btnLinkPayPal.SetLeftImage("paypal_icon.png");
            btnUnlinkPayPal.SetTitle(Localize.GetValue("UnlinkPayPal"), UIControlState.Normal);
            btnUnlinkPayPal.TouchUpInside += (sender, e) => ViewModel.UnlinkPayPalAccount();

            // Add horizontal separator
            if (!ViewModel.IsPayPalOnly)
            {
                viewPayPal.AddSubview(Line.CreateHorizontal(8f, 0f, viewPayPal.Frame.Width - (2*8f), UIColor.Black, 1f));
            }
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            // ugly fix for iOS 7 bug with horizontal scrolling
            // unlike iOS 8, the contentSize is a bit larger than the view, resulting in an undesired horizontal bounce
            if (UIHelper.IsOS7)
            {
                var scrollView = (UIScrollView)View.Subviews[0];
                if (scrollView.ContentSize.Width > UIScreen.MainScreen.Bounds.Width)
                {
                    scrollView.ContentSize = new CoreGraphics.CGSize(UIScreen.MainScreen.Bounds.Width, scrollView.ContentSize.Height);
                }
            }
        }

        private bool GoToNext (UITextField textField)
        {
            txtNameOnCard.ResignFirstResponder ();
            txtCardNumber.BecomeFirstResponder();
            return true;
        }

        private void PayPalFlow()
        {
            if (_payPalPayment == null)
            {
                _payPalPaymentDelegate = new PayPalDelegate(authCode => ViewModel.LinkPayPalAccount(authCode));
                _payPalPayment = new PayPalCustomFuturePaymentViewController((PayPalConfiguration)Mvx.Resolve<IPayPalConfigurationService>().GetConfiguration(), _payPalPaymentDelegate);
            }

            if (!ViewModel.IsAddingNewCard)
            {
                this.Services().Message.ShowMessage(
                    this.Services().Localize["DeleteCreditCardTitle"],
                    this.Services().Localize["LinkPayPalCCWarning"],
                    this.Services().Localize["LinkPayPalConfirmation"], () =>
                    {
                        PresentViewController(_payPalPayment, true, null);
                    },
                    this.Services().Localize["Cancel"], () => { });
            }
            else
            {
                PresentViewController(_payPalPayment, true, null);
            }
        }

        private void ScanCard ()
        {           
            if (_cardScanner == null)
            {
                _cardScannerDelegate = new CardScannerDelegate(PopulateCreditCardName);
                _cardScanner = new CardIOPaymentViewController(_cardScannerDelegate)
                {
                    GuideColor = this.View.BackgroundColor,
                    SuppressScanConfirmation = true,
                    CollectCVV = false,
                    CollectExpiry = false,
                    DisableManualEntryButtons = true,
                    DisableBlurWhenBackgrounding = true,
                    AutomaticallyAdjustsScrollViewInsets = false,
                    HideCardIOLogo = true,
                };
            }

            PresentViewController(_cardScanner, true, null);
        }

		private void PopulateCreditCardName(CreditCardInfo info)
        {
            txtCardNumber.Text = info.CardNumber;
            ViewModel.CreditCardNumber = info.CardNumber;
            txtCvv.BecomeFirstResponder();
        }

        private class CardScannerDelegate : CardIOPaymentViewControllerDelegate
        {
            private Action<CreditCardInfo> _cardScanned;

            public CardScannerDelegate (Action<CreditCardInfo> cardScanned)
            {
                _cardScanned = cardScanned;
            }

            public override void UserDidCancelPaymentViewController(CardIOPaymentViewController paymentViewController)
            {
                paymentViewController.DismissViewController(true, null);
            }

            public override void UserDidProvideCreditCardInfo(CreditCardInfo cardInfo, CardIOPaymentViewController paymentViewController)
            {
                _cardScanned(cardInfo);
                paymentViewController.DismissViewController(true, null);
            }
        }

        private class PayPalDelegate : PayPalFuturePaymentDelegate
        {
            private readonly Action<string> _futurePaymentAuthorized;

            public PayPalDelegate (Action<string> futurePaymentAuthorized)
            {
                _futurePaymentAuthorized = futurePaymentAuthorized;
            }

            public override void DidCancelFuturePayment(PayPalFuturePaymentViewController futurePaymentViewController)
            {
                Logger.LogMessage("PayPal LinkAccount: The user canceled the operation");
                futurePaymentViewController.DismissViewController(true, null);
            }

            public override void DidAuthorizeFuturePayment(PayPalFuturePaymentViewController futurePaymentViewController, NSDictionary futurePaymentAuthorization)
            {
                // The user has successfully logged into PayPal, and has consented to future payments.
                // Your code must now send the authorization response to your server.
                try
                {
                    NSError error;
                    var contentJsonData = NSJsonSerialization.Serialize(futurePaymentAuthorization, NSJsonWritingOptions.PrettyPrinted, out error);

                    if (error != null)
                    {
                        throw new Exception(error.LocalizedDescription + " " + error.LocalizedFailureReason);
                    }

                    var authResponse = contentJsonData.ToString().FromJson<FuturePaymentAuthorization>();
                    if (authResponse != null)
                    {
                        _futurePaymentAuthorized(authResponse.Response.Code);
                    }

                    // Be sure to dismiss the PayPalLoginViewController.
                    futurePaymentViewController.DismissViewController(true, null);
                }
                catch(Exception e)
                {
                    Logger.LogError(e);
                    Mvx.Resolve<IMessageService>().ShowMessage(Mvx.Resolve<ILocalization>()["Error"], e.GetBaseException().Message);
                }
            }
        }

        private class FuturePaymentAuthorization
        {
            public FuturePaymentAuthorization()
            {
                Response = new FuturePaymentAuthorizationResponse();
            }

            public FuturePaymentAuthorizationResponse Response { get; set; }

            public class FuturePaymentAuthorizationResponse
            {
                public string Code { get; set; }
            }
        }

        private class PayPalCustomFuturePaymentViewController : PayPalFuturePaymentViewController
        {
            public PayPalCustomFuturePaymentViewController(PayPalConfiguration configuration, PayPalFuturePaymentDelegate futurePaymentDelegate)
                : base(configuration, futurePaymentDelegate)
            {
            }

            public override void ViewWillAppear(bool animated)
            {
                base.ViewWillAppear(animated);

                // change navbar colors to PayPal light blue so we see it on the white background
                var payPalLightBlue = UIColor.FromRGB(40, 155, 228);
                ChangeNavBarButtonColor(payPalLightBlue);
            }

            public override void ViewWillDisappear(bool animated)
            {
                base.ViewWillDisappear(animated);

                // revert navbar colors
                ChangeNavBarButtonColor(Theme.LabelTextColor);
            }

            private void ChangeNavBarButtonColor(UIColor textColor)
            {
                var buttonFont = UIFont.FromName (FontName.HelveticaNeueLight, 34/2);

                // set back/left/right button color
                var buttonTextColor = new UITextAttributes 
                {
                    Font = buttonFont,
                    TextColor = textColor,
                    TextShadowColor = UIColor.Clear,
                    TextShadowOffset = new UIOffset(0,0)
                };
                var selectedButtonTextColor = new UITextAttributes
                {
                    Font = buttonFont,
                    TextColor = textColor.ColorWithAlpha(0.5f),
                    TextShadowColor = UIColor.Clear,
                    TextShadowOffset = new UIOffset(0,0)
                };

                UIBarButtonItem.Appearance.SetTitleTextAttributes(buttonTextColor, UIControlState.Normal);
                UIBarButtonItem.Appearance.SetTitleTextAttributes(selectedButtonTextColor, UIControlState.Highlighted);
                UIBarButtonItem.Appearance.SetTitleTextAttributes(selectedButtonTextColor, UIControlState.Selected);
            }
        }
    }
}


