using System;
using System.Collections.Generic;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Extensions;
using System.Linq;
using apcurium.MK.Booking.Api.Contract.Requests.Payment;

namespace apcurium.MK.Booking.Api.Client.TaxiHail
{
    public class ConfigurationClientService : BaseServiceClient, IConfigurationManager
    {
        private static Dictionary<string, string> _settings = null;

        public ConfigurationClientService (string url, string sessionId)
            : base(url, sessionId)
        {
        }

        public void Reset ()
        {
            _settings = null;
        }

        public string GetSetting (string key)
        {
            if ((_settings == null) || (!_settings.ContainsKey (key))) {
                Load ();
            }

            if (!_settings.ContainsKey (key)) {
                return null;
            }

            return _settings [key];
        }

        private void Load ()
        {            
            var settings = Client.Get<Dictionary<string,string>> ("/settings");
            var dict = new Dictionary<string, string> ();
            settings.ForEach (s => dict.Add (s.Key, s.Value));
            _settings = dict;
        }

        public void SetSetting (string key, string value)
        {
            throw new NotImplementedException ();
        }

        public IDictionary<string, string> GetSettings ()
        {
            if (_settings == null) {
                Load ();
            }
            return _settings;            
        }

        public void SetSettings (IDictionary<string, string> appSettings)
        {
            throw new NotImplementedException ();
        }

		ClientPaymentSettings _cachedSettings = null;


        public ClientPaymentSettings GetPaymentSettings(bool cleanCache = false)
		{
			if(_cachedSettings == null || cleanCache)
			{
				_cachedSettings = Client.Get (new PaymentSettingsRequest ()).ClientPaymentSettings;
			}
			return _cachedSettings;
        }
    }
}
