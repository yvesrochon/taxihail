﻿using System.Collections.Generic;
using MK.Common.Android.Configuration.Impl;

namespace apcurium.MK.Common.Configuration
{
    public interface IConfigurationManager
    {
        void Reset();
        string GetSetting( string key );
        
        IDictionary<string, string> GetSettings();
        void SetSettings(IDictionary<string, string> appSettings);


        ClientPaymentSetting GetPaymentSettings();
    }

    public class ClientPaymentSetting
    {
        public PaymentMethod PaymentMode { get; set; }
    }
}
