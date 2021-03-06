﻿using System;
using System.ComponentModel.DataAnnotations;

namespace apcurium.MK.Booking.ReadModel
{
    public class AppStartUpLogDetail
    {
        [Key]
        public Guid UserId { get; set; }

        /// <summary>
        /// This date is saved in UTC
        /// </summary>
        public DateTime DateOccured { get; set; }

        public string ApplicationVersion { get; set; }

        public string Platform { get; set; }

        public string PlatformDetails { get; set; }

        public string ServerVersion { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}