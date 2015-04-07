using System;
using System.Threading.Tasks;
using apcurium.MK.Booking.Api.Client.Extensions;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Api.Contract.Requests.Payment;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Resources;

namespace apcurium.MK.Booking.Api.Client.TaxiHail
{
    public class ManualPairingForRideLinqServiceClient : BaseServiceClient
    {
        public Task<OrderManualRideLinqDetail> Pair(ManualRideLinqPairingRequest manualRideLinqPairingRequest)
        {
            var req = string.Format("/account/ridelinq");
            return Client.PostAsync<OrderManualRideLinqDetail>(req, manualRideLinqPairingRequest);
        }

        public Task<BasePaymentResponse> Unpair(Guid orderId)
        {
            var req = string.Format("/account/ridelinq/{0}", orderId);
            return Client.DeleteAsync<BasePaymentResponse>(req);
        }

        public Task<OrderManualRideLinqDetail> GetUpdatedTrip(Guid orderId)
        {
            var req = string.Format("/account/ridelinq/{0}", orderId);
            return Client.GetAsync<OrderManualRideLinqDetail>(req);
        }

    }
}