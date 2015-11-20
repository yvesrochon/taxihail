﻿using ServiceStack.ServiceHost;

namespace apcurium.MK.Booking.Api.Contract.Requests
{
    [Route("/roaming/marketsettings", "GET")]
    public class FindMarketSettingsRequest
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
