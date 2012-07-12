﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;

namespace apcurium.MK.Booking.Api.Contract.Requests
{
    [RestService("/accounts/{AccountId}/orders/{OrderId}/cancel", "POST")]
    public class CancelOrder
    {

        public Guid AccountId { get; set; }
        public Guid OrderId { get; set; }
    }
}
