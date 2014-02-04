using System;
using Cirrious.MvvmCross.Views;
using Cirrious.MvvmCross.Droid.Views;
using Android.Content;
using Android.App;
using Cirrious.CrossCore.Platform;
using apcurium.MK.Booking.Mobile.ViewModels.Orders;
using apcurium.MK.Booking.Mobile.Client.Activities.Book;

namespace apcurium.MK.Booking.Mobile.Client
{
    public class PhonePresenter : MvxAndroidViewPresenter
    {
        public PhonePresenter()
        {
        }

        public override void Show(Cirrious.MvvmCross.ViewModels.MvxViewModelRequest request)
        {
            bool removeFromHistory = request.ParameterValues != null
                                     && request.ParameterValues.ContainsKey("removeFromHistory");

            Intent intent = this.CreateIntentForRequest (request);
            this.Show (intent, removeFromHistory);
        }

        public override void ChangePresentation(Cirrious.MvvmCross.ViewModels.MvxPresentationHint hint)
        {
            if (hint is ShowOrderReviewPresentationHint)
            {
                TryShowOrderReview();
            }
            else
            {
                base.ChangePresentation(hint);
            }
        }

        private void Show (Intent intent, bool removeFromHistory)
        {
            Activity activity = this.Activity;
            if (activity == null)
            {
                MvxTrace.Warning ("Cannot Resolve current top activity", new object[0]);
                return;
            }
            activity.StartActivity (intent);
            if (removeFromHistory)
            {
                activity.Finish();
            }
        }

        private void TryShowOrderReview()
        {
            var homeView = Activity as HomeActivity;
            if (homeView != null)
            {
                homeView.ShowOrderReview();
            }
            else
            {
                MvxTrace.Warning("Can't show order review");
            }

        }
    }
}

