﻿#region

using System;
using System.Collections.Generic;
using apcurium.MK.Booking.Api.Contract.Http;
using apcurium.MK.Booking.Api.Contract.Validation;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Common.Entity;
using ServiceStack.ServiceHost;

#endregion

namespace apcurium.MK.Booking.Api.Contract.Requests
{
    [Route("/admin/addresses", "GET")]
    [Route("/admin/addresses", "POST")]
    [Route("/admin/addresses/{Id}", "PUT, DELETE")]
    public class DefaultFavoriteAddress : BaseDto
    {
        public Guid Id { get; set; }

        [AddressLatitudeValidation(MinLatitude = -90d, MaxLatitude = 90d),
         AddressLongitudeValidation(MinLongitude = -180d, MaxLongitude = 180d)]
        public Address Address { get; set; }
    }

    [NoCache]
    public class DefaultFavoriteAddressResponse : List<DefaultAddressDetails>
    {
        public DefaultFavoriteAddressResponse()
        {
        }

        public DefaultFavoriteAddressResponse(IEnumerable<DefaultAddressDetails> collection)
            : base(collection)
        {
        }
    }
}