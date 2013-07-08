﻿using System;
using System.ComponentModel.DataAnnotations;

namespace apcurium.MK.Common.Configuration.Impl
{
    public class PayPalServerSettings
    {
        public PayPalServerSettings() { }

        public PayPalServerSettings(Guid id)
        {
            Id = id;
            SandboxCredentials = new PayPalCredentials();
            Credentials = new PayPalCredentials();
            IsSandbox = true;
        }
  

        [Key]
        public Guid Id { get; set; }
        public bool IsSandbox {get; set;}
        public PayPalCredentials SandboxCredentials { get; set; }
        public PayPalCredentials Credentials { get; set; }
    }
}
