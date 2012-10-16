﻿using System;

namespace apcurium.MK.Booking.Api.Contract.Resources
{

    public class Account : BaseDTO  
    {
        public Guid Id { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }        

        public string Phone { get; set; }

        public int IBSAccountid { get; set; }

        public string FacebookId { get; set; }

        public string TwitterId { get; set; }

        public BookingSettings Settings { get; set; }

        public string Language { get; set; }

        public bool IsAdmin { get; set; }
    }
}
