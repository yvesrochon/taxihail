﻿using System;

using Foundation;
using UIKit;
using apcurium.MK.Booking.Mobile.ViewModels;
using Cirrious.MvvmCross.Binding.BindingContext;
using apcurium.MK.Common.Extensions;

namespace apcurium.MK.Booking.Mobile.Client.Views
{
	public partial class ManualPairingForRideLinqView : BaseViewController<ManualPairingForRideLinqViewModel>
	{
		public ManualPairingForRideLinqView()
			: base("ManualPairingForRideLinqView", null)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			FlatButtonStyle.Green.ApplyTo(btnPair);

			var localize = this.Services().Localize;

			NavigationController.NavigationBar.Hidden = false;
			NavigationItem.Title = localize["View_RideLinqPair"];

			lblInstructions.Text = localize["ManualPairingForRideLinQ_Instructions"];

			btnPair.SetTitle(localize["ManualPairingForRideLinQ_Pair"], UIControlState.Normal);

			FlatButtonStyle.Green.ApplyTo(btnPair);

			var bindingSet = this.CreateBindingSet<ManualPairingForRideLinqView, ManualPairingForRideLinqViewModel>();

			bindingSet.Bind(PairingCode1)
				.To(vm => vm.PairingCodeLeft);

			bindingSet.Bind(PairingCode2)
				.To(vm => vm.PairingCodeRight);

			bindingSet.Bind(btnPair)
				.For(v => v.Command)
				.To(vm => vm.PairWithRideLinq);

			bindingSet.Apply();

			PairingCode1.ShouldChangeCharacters = CheckMaxLength3;

			PairingCode2.ShouldChangeCharacters = CheckMaxLength4;

			PairingCode1.EditingChanged += (object sender, EventArgs e) => 
			{
				if(PairingCode1.Text.Length == 3)
				{
					PairingCode2.BecomeFirstResponder();
				}
			};

			PairingCode2.EditingChanged += (object sender, EventArgs e) => 
			{
				if(PairingCode2.Text.Length == 0)
				{
					PairingCode1.BecomeFirstResponder();
				}
			};
					
		}

		private bool CheckMaxLength3 (UITextField textField, NSRange range, string replacementString)
		{
			nint textLength = textField.Text.HasValue() ? textField.Text.Length : 0;
			nint replaceLength = replacementString.HasValue () ? replacementString.Length : 0;
			nint newLength = textLength + replaceLength - range.Length;
			return newLength <= 3;
		}

		private bool CheckMaxLength4 (UITextField textField, NSRange range, string replacementString)
		{
			nint textLength = textField.Text.HasValue () ? textField.Text.Length : 0;
			nint replaceLength = replacementString.HasValue () ? replacementString.Length : 0;
			nint newLength = textLength + replaceLength - range.Length;
			return newLength <= 4;
		}
	}
}

