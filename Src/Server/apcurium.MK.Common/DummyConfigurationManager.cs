﻿using System;
using System.Collections.Generic;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Configuration.Helpers;
using apcurium.MK.Common.Configuration.Impl;

namespace apcurium.MK.Common
{
    public class DummyServerSettings : IServerSettings
    {
        public DummyServerSettings(Dictionary<string, string> dictionary = null)
        {
            if (dictionary == null)
            {
                dictionary = new Dictionary<string, string>();
            }

            AppSettings = dictionary;

            ServerData = new ServerTaxiHailSetting();
            Load();
        }
        public void Load()
        {
            SettingsLoader.InitializeDataObjects(ServerData, GetSettings(), null);
        }
        private IDictionary<string, string> AppSettings { get; set; }

        public ServerTaxiHailSetting ServerData { get; private set; }

        public IDictionary<string, string> GetSettings()
        {
            return AppSettings;
        }

        public ServerPaymentSettings GetPaymentSettings(string companyKey = null)
        {
            throw new NotImplementedException();
        }

        public void Reload()
        {
        }

        public void AddOrSet(string key, string value)
        {
            if (AppSettings.ContainsKey(key))
            {
                AppSettings[key] = value;
            }
            else
            {
                AppSettings.Add(key, value);
            }
        }
    }
}