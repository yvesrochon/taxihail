﻿using System;

namespace apcurium.MK.Booking.Api.Contract.Resources
{
    public class Address : BaseDTO
    {
        private Guid _internalId = Guid.NewGuid();

        public Guid Id { get; set; }

        public string FriendlyName { get; set; }

        public string StreetNumber { get; set; }
        
        public string Street { get; set; }
        
        public string City { get; set; }

        public string ZipCode { get; set; }

        public string FullAddress { get; set; }        

        public double Longitude { get; set; }

        public double Latitude { get; set; }

        public string Apartment { get; set; }

        public string RingCode { get; set; }

        public bool IsHistoric { get; set; }

        public string AddressType { get; set; }

    }
}
