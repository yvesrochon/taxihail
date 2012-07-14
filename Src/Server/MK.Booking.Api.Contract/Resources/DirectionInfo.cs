﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace apcurium.MK.Booking.Api.Contract.Resources
{
    public class DirectionInfo
    {
        public int? Distance { get; set; }
        public double? Price { get; set; }

        public string FormattedPrice { get; set; }
        public string FormattedDistance { get; set; }
    }
}
