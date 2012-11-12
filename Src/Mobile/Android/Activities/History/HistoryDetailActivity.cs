﻿using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using apcurium.Framework.Extensions;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Booking.Mobile.Client.Helpers;
using apcurium.MK.Booking.Mobile.Messages;
using apcurium.MK.Booking.Mobile.ViewModels;
using Cirrious.MvvmCross.Interfaces.ViewModels;
using Cirrious.MvvmCross.Interfaces.Views;
using Cirrious.MvvmCross.Views;
using System;
using System.Collections.Generic;
using TinyIoC;
using TinyMessenger;

namespace apcurium.MK.Booking.Mobile.Client.Activities.History
{
    [Activity(Label = "History Details", Theme = "@android:style/Theme.NoTitleBar", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class HistoryDetailActivity : BaseBindingActivity<HistoryDetailViewModel>
    {
        private TinyMessageSubscriptionToken _closeViewToken;       

        protected override int ViewTitleResourceId
        {
            get { return Resource.String.View_HistoryDetail; }
        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            _closeViewToken = TinyIoCContainer.Current.Resolve<ITinyMessengerHub>().Subscribe<CloseViewsToRoot>(m => Finish());            
        } 

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_closeViewToken != null)
            {
                TinyIoCContainer.Current.Resolve<ITinyMessengerHub>().Unsubscribe<CloseViewsToRoot>(_closeViewToken);
            }
        }

        protected override void OnViewModelSet()
        {
            SetContentView(Resource.Layout.View_HistoryDetail);
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.Initialize();

            var btnCancel = FindViewById<Button>(Resource.Id.CancelTripBtn);
            var btnStatus = FindViewById<Button>(Resource.Id.StatusBtn);
            var btnRebook = FindViewById<Button>(Resource.Id.RebookTripBtn);
            var btnSendReceipt = FindViewById<Button>(Resource.Id.SendReceiptBtn);
            var btnDelete = FindViewById<Button>(Resource.Id.HistoryOrderDeleteBtn);

            btnCancel.Visibility = ViewStates.Gone;
            btnStatus.Visibility = ViewStates.Gone;
            btnSendReceipt.Visibility = ViewStates.Gone;

            btnCancel.Click += new EventHandler(btnCancel_Click);
            btnStatus.Click += new EventHandler(btnStatus_Click);
            btnRebook.Click += new EventHandler(btnRebook_Click);
            btnSendReceipt.Click += new EventHandler(btnSendReceipt_Click);
            btnDelete.Click += new EventHandler(btnDelete_Click);
            FindViewById<TextView>(Resource.Id.StatusTxt).Text = Resources.GetString(Resource.String.LoadingMessage);

        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Order")
            {
                UpdateUI();
            } 
            else if(e.PropertyName == "Status")
            {
                RefreshStatus();
            }
        }

        private void UpdateUI()
        {
            RunOnUiThread(() => {
                var order = ViewModel.Order;
                FindViewById<TextView>(Resource.Id.ConfirmationTxt).Text = order.IBSOrderId.HasValue ? order.IBSOrderId.Value.ToString() : "Error";
                FindViewById<TextView>(Resource.Id.RequestedTxt).Text = FormatDateTime(order.PickupDate, order.PickupDate);
                FindViewById<TextView>(Resource.Id.OriginTxt).Text = order.PickupAddress.FullAddress;
                FindViewById<TextView>(Resource.Id.AptRingTxt).Text = FormatAptRingCode(order.PickupAddress.Apartment, order.PickupAddress.RingCode);
                FindViewById<TextView>(Resource.Id.DestinationTxt).Text = order.DropOffAddress.FullAddress.HasValue() ? order.DropOffAddress.FullAddress : Resources.GetString(Resource.String.ConfirmDestinationNotSpecified);
                FindViewById<TextView>(Resource.Id.PickUpDateTxt).Text = FormatDateTime(order.PickupDate, order.PickupDate);
            });
        }

        void btnRebook_Click(object sender, EventArgs e)
        {
            
            TinyIoCContainer.Current.Resolve<ITinyMessengerHub>().Publish(new RebookRequested(this, ViewModel.Order));
            TinyIoCContainer.Current.Resolve<ITinyMessengerHub>().Publish(new CloseViewsToRoot(this));
        }

        void btnSendReceipt_Click(object sender, EventArgs e)
        {
            ThreadHelper.ExecuteInThread(this, () =>
            {
                if (Common.Extensions.GuidExtensions.HasValue(ViewModel.OrderId))
                {
                    TinyIoCContainer.Current.Resolve<IBookingService>().SendReceipt(ViewModel.OrderId);
                }

                RunOnUiThread(Finish);
            }, true);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            ThreadHelper.ExecuteInThread(this, () =>
            {
                if (Common.Extensions.GuidExtensions.HasValue(ViewModel.OrderId))
                {
                    TinyIoCContainer.Current.Resolve<IBookingService>().RemoveFromHistory(ViewModel.OrderId);
                }

                RunOnUiThread(Finish);
            }, true);
        }


        void btnStatus_Click(object sender, EventArgs e)
        {

            /*Intent i = new Intent(this, typeof(BookingStatusActivity));

            OrderStatusDetail orderInfo = new OrderStatusDetail { IBSOrderId = _data.IBSOrderId, IBSStatusDescription = "Loading...", IBSStatusId = "", OrderId = _data.Id, Status = OrderStatus.Unknown, VehicleLatitude = null, VehicleLongitude = null };

            var serialized = _data.Serialize();
            i.PutExtra("Order", serialized);

            serialized = orderInfo.Serialize();
            i.PutExtra("OrderStatusDetail", serialized);


            StartActivityForResult(i, 101);*/
            var orderInfo = new OrderStatusDetail { IBSOrderId = ViewModel.Order.IBSOrderId, IBSStatusDescription = "Loading...", IBSStatusId = "", OrderId = ViewModel.OrderId, Status = OrderStatus.Unknown, VehicleLatitude = null, VehicleLongitude = null };
            var param = new Dictionary<string, object>() { { "order", ViewModel.Order }, { "orderInfo", orderInfo } };
            ViewModel.NavigateToOrderStatus.Execute(param);
            //Intent intent = new Intent();
            //intent.SetFlags(ActivityFlags.ForwardResult);
            //intent.PutExtra("Book", _data.Id.ToString());
            //SetResult(Result.Ok, intent);
            //Finish();
        }

        void btnCancel_Click(object sender, EventArgs e)
        {
            var newBooking = new Confirmation();
            newBooking.Action(this, Resource.String.StatusConfirmCancelRide, () =>
            {
                ThreadHelper.ExecuteInThread(this, () =>
                {
                    var isSuccess = TinyIoCContainer.Current.Resolve<IBookingService>().CancelOrder(ViewModel.OrderId);
                    if (isSuccess)
                    {
                        RefreshStatus();
                    }
                    else
                    {
                        RunOnUiThread(() =>
                            {
                                this.ShowAlert(Resources.GetString(Resource.String.StatusConfirmCancelRideErrorTitle), Resources.GetString(Resource.String.StatusConfirmCancelRideError));
                            });
                    }

                }, false);
            });
        }

        private void RefreshStatus()
        {
            bool isCompleted = false;
            if (ViewModel.Status.IBSStatusId.HasValue())
            {
                isCompleted = TinyIoCContainer.Current.Resolve<IBookingService>().IsStatusCompleted(ViewModel.Status.IBSStatusId);
            }

            RunOnUiThread(() =>
            {
                FindViewById<TextView>(Resource.Id.StatusTxt).Text = ViewModel.Status.IBSStatusDescription;
                var btnCancel = FindViewById<Button>(Resource.Id.CancelTripBtn);
                var btnStatus = FindViewById<Button>(Resource.Id.StatusBtn);
                var btnDelete = FindViewById<Button>(Resource.Id.HistoryOrderDeleteBtn);
                var btnSendReceipt = FindViewById<Button>(Resource.Id.SendReceiptBtn);

                btnCancel.Visibility = isCompleted ? ViewStates.Gone : ViewStates.Visible;
                btnStatus.Visibility = isCompleted ? ViewStates.Gone : ViewStates.Visible;
                btnDelete.Visibility = isCompleted ? ViewStates.Visible : ViewStates.Gone;
                btnSendReceipt.Visibility = ViewModel.Status.FareAvailable ? ViewStates.Visible : ViewStates.Gone;
            });
        }

        private string FormatDateTime(DateTime? date, DateTime? time)
        {
            string result = date.HasValue ? date.Value.ToShortDateString() : Resources.GetString(Resource.String.DateToday);
            result += @" / ";
            result += time.HasValue ? time.Value.ToShortTimeString() : Resources.GetString(Resource.String.TimeNow);
            return result;
        }

        private string FormatAptRingCode(string apt, string rCode)
        {
            string result = apt.HasValue() ? apt : Resources.GetString(Resource.String.ConfirmNoApt);

            result += @" / ";
            result += rCode.HasValue() ? rCode : Resources.GetString(Resource.String.ConfirmNoRingCode);
            return result;
        }
    }
}
