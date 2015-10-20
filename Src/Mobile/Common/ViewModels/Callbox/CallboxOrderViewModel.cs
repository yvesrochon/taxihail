using System;
using System.Windows.Input;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Booking.Mobile.Extensions;
using apcurium.MK.Booking.Mobile.Messages;
using apcurium.MK.Common.Entity;

namespace apcurium.MK.Booking.Mobile.ViewModels.Callbox
{
    public class CallboxOrderViewModel : BaseViewModel
    {
        private readonly IBookingService _bookingService;
	    private int? _ibsOrderId;
	    private OrderStatusDetail _orderStatus;

	    public CallboxOrderViewModel(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }
            
        public Guid Id { get; set; }

	    public int? IbsOrderId
	    {
		    get { return _ibsOrderId; }
		    set
		    {
			    _ibsOrderId = value;
			    RaisePropertyChanged();
				RaisePropertyChanged(() => IbsOrderIdString);
		    }
	    }

	    public DateTime CreatedDate { get; set; }

	    public OrderStatusDetail OrderStatus
	    {
		    get { return _orderStatus; }
		    set
		    {
			    _orderStatus = value; 
			    RaisePropertyChanged();
		    }
	    }

	    public string FormatedCreatedDate
        {
            get {  return CreatedDate.ToShortTimeString(); }
        }

        public string IbsOrderIdString
        {
            get { return IbsOrderId.ToString(); }
        }

        public ICommand CancelOrder
        {
            get
            {
				return this.GetCommand(() => this.Services().MessengerHub.Publish(new OrderDeleted(this, Id, null)));
            }
        }

        public bool CanBeCancelled
        {
            get { return !_bookingService.IsCallboxStatusCompleted(OrderStatus.IBSStatusId); }
        }

        public ColorEnum TextColor
        {
            get
            {
				if (OrderStatus.IBSStatusId != null)
                {
					switch (OrderStatus.IBSStatusId)
                    {
                        case "wosWAITING":
                            return ColorEnum.LightGray;
                        case "wosARRIVED":
                            return ColorEnum.Green;
                        case "wosASSIGNED":
                            return ColorEnum.Black;
                    }
                }
                return ColorEnum.LightGray;
            }
        }

        public int TextSize
        {
            get
            {
				if (OrderStatus.IBSStatusId != null && OrderStatus.IBSStatusId.Equals("wosARRIVED"))
                {
                    return 25;
                }
                return 18;
            }
        }
    }
}  