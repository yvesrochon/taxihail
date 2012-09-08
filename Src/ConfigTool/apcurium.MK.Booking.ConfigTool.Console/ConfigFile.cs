﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace apcurium.MK.Booking.ConfigTool
{
    public class ConfigFile : Config
    {
        public string Source { get; set; }
        public string Destination{ get; set; }

        public ConfigFile(AppConfig parent)
            : base(parent)
        {
        }

        public override void Apply()
        {
            var destPath = Path.Combine(Parent.SrcDirectoryPath, Destination);
            var sourcePath = Path.Combine(Parent.ConfigDirectoryPath, Source);
            File.Copy(sourcePath, destPath, true);
        }
        
    }

}
