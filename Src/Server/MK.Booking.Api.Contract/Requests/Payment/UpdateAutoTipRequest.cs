﻿using System;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace apcurium.MK.Booking.Api.Contract.Requests.Payment
{
    [Authenticate]
    [Route("/account/orders/{OrderId}/pairing/tip", "POST")]
    public class UpdateAutoTipRequest : BaseDto
    {
        public Guid OrderId { get; set; }

        public int AutoTipPercentage { get; set; }
    }
}
