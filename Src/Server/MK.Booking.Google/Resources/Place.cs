﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace apcurium.MK.Booking.Google.Resources
{
    public class Place
    {
        public Geometry Geometry { get; set; }
        public string Icon { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public float Rating { get; set; }
        public string Reference { get; set; }
        public List<string> Types { get; set; }
        public string Vicinity { get; set; }
        public List<Event> Events { get; set; }
    }
}
