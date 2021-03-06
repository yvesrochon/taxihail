﻿using apcurium.MK.Common.Resources;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace apcurium.MK.Booking.Api.Contract.Requests.Payment
{
    [Authenticate]
    [Route("/payments/settleoverduepayment", "POST")]
    public class SettleOverduePaymentRequest : IReturn<SettleOverduePaymentResponse>
    {
        public string KountSessionId { get; set; }

        public string CustomerIpAddress { get; set; }
    }
}