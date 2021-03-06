using Android.Content;
using Android.Util;
using Android.Widget;
using apcurium.MK.Booking.Mobile.ViewModels.Orders;
using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Binding.Droid.Views;
using Android.Views;
using apcurium.MK.Booking.Mobile.Client.Helpers;
using apcurium.MK.Booking.Mobile.Client.Controls.Behavior;
using System.Collections.Generic;
using Android.Graphics;
using Android.Runtime;
using System;

namespace apcurium.MK.Booking.Mobile.Client.Controls.Widgets
{
	[Register("apcurium.MK.Booking.Mobile.Client.Controls.Widgets.OrderAirport")]
    public class OrderAirport : MvxFrameControl
    {
        private TextView _txtDateTime;
		private EditTextSpinner _txtAirlines;
        private EditTextSpinner _txtPUPoints;
        private EditText _txtFlightNum;
        private EditTextEntry _txtEditNote;
        private LinearLayout _bottomPadding;

		private bool _isShown;
		private ViewStates _animatedVisibility;

		public OrderAirport(Context context, IAttributeSet attrs) : base (LayoutHelper.GetLayoutForView(Resource.Layout.SubView_OrderAirport, context), context, attrs)
		{
            this.DelayBind(() => 
            {
				_txtPUPoints = Content.FindViewById<EditTextSpinner>(Resource.Id.txtPUPoints);
                _txtAirlines = Content.FindViewById<EditTextSpinner>(Resource.Id.txtAirlines);
                _txtFlightNum = Content.FindViewById<EditText>(Resource.Id.txtFlightNum);
                _txtEditNote = FindViewById<EditTextEntry>(Resource.Id.txtAirportNotes);
                _txtDateTime = Content.FindViewById<Button>(Resource.Id.txtDateTime);

                _txtEditNote.SetClickAnywhereToDismiss();

                // hack for scroll in view when in EditText
                _bottomPadding = Content.FindViewById<LinearLayout>(Resource.Id.HackBottomPadding);
                TextFieldInHomeSubviewsBehavior.ApplyTo(
                    new List<EditText>() { _txtEditNote, _txtFlightNum },
                    () => _bottomPadding.Visibility = ViewStates.Visible,
                    () => _bottomPadding.Visibility = ViewStates.Gone
                );
                var hintTextColor = Resources.GetColor(Resource.Color.drivernode_hint_color);

                _txtEditNote.SetHintTextColor(hintTextColor);
                _txtDateTime.SetHintTextColor(hintTextColor);

                InitializeBinding();
            });
        }

		public void ShowWithoutAnimation()
		{
			_isShown = true;

			if (Animation != null)
			{
				Animation.Cancel();
			}

			((MarginLayoutParams)LayoutParameters).TopMargin = OrderAirportShownHeightProvider();
		}

		public Point ScreenSize { get; set; }

		public Func<int> OrderAirportShownHeightProvider { get; set; }

		public Func<int> OrderAirportHiddenHeightProvider { get; set; }


		public ViewStates AnimatedVisibility
		{
			get { return _animatedVisibility; }
			set
			{
				_animatedVisibility = value;
				if (value == ViewStates.Visible)
				{
					ShowIfNeeded();
					return;
				}

				HideIfNeeded();
			}
		}

		private void ShowIfNeeded()
		{
			_isShown = true;

			var animation = AnimationHelper.GetForYTranslation(this, OrderAirportShownHeightProvider());
			animation.AnimationStart += (sender, e) =>
			{
				// set it to fill_parent to allow the subview to take the remaining space in the screen 
				// and to allow the view to resize when keyboard is up
				if (((MarginLayoutParams)LayoutParameters).Height != ViewGroup.LayoutParams.MatchParent)
				{
					((MarginLayoutParams)LayoutParameters).Height = ViewGroup.LayoutParams.MatchParent;
				}
			};

			StartAnimation(animation);
		}

		private void HideIfNeeded()
		{
			if (!_isShown)
			{
				return;
			}

			_isShown = false;

			var animation = AnimationHelper.GetForYTranslation(this, ScreenSize.Y);
			animation.AnimationEnd += (sender, e) =>
			{
				var desiredHeight = OrderAirportHiddenHeightProvider();
				// reset to a fix height in order to have a smooth translation animation next time we show the review screen
				if (((MarginLayoutParams)LayoutParameters).Height != desiredHeight)
				{
					((MarginLayoutParams)LayoutParameters).Height = desiredHeight;
				}
			};

			StartAnimation(animation);
		}


        void InitializeBinding()
		{
            var set = this.CreateBindingSet<OrderAirport, OrderAirportViewModel>();

            set.Bind(_txtDateTime)
                .For(v => v.Text)
                .To(vm => vm.PickupTimeStamp);

            set.Bind(_txtDateTime)
                .For("Click")
                .To(vm => vm.NavigateToDatePicker);

            set.Bind(_txtFlightNum)
                .For(v => v.Text)
                .To(vm => vm.FlightNumber );

            set.Bind(_txtAirlines)
                .For("Text")
                .To(vm => vm.AirlineName);

            set.Bind(_txtAirlines)
                .For("Data")
                .To(vm => vm.Airlines);

            set.Bind(_txtAirlines)
                .For("SelectedItem")
                .To(vm => vm.AirlineId);

            set.Bind(_txtEditNote)
                .For(v => v.Text)
                .To(vm => vm.Note);

            set.Bind(_txtPUPoints)
                .For("Text")
                .To(vm => vm.SelectedPickupPointName);

            set.Bind(_txtPUPoints)
                .For("Data")
                .To(vm => vm.PickupPoints);

            set.Bind(_txtPUPoints)
                .For("SelectedItem")
                .To(vm => vm.SelectedPickupPointsId);
            
            set.Apply();
		}
    }
}
      

