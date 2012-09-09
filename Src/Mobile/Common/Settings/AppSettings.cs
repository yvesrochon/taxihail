using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using apcurium.MK.Booking.Mobile.Infrastructure;
using ServiceStack.Text;
using System.Reflection;
using System.IO;

namespace apcurium.MK.Booking.Mobile.Settings
{

    public class AppSettings : IAppSettings
    {

        private AppSettingsData _data;


        public AppSettings()
        {

            using (var stream = this.GetType().Assembly.GetManifestResourceStream("apcurium.MK.Booking.Mobile.Settings.Settings.json"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    
                    string serializedData = reader.ReadToEnd();
                    _data = JsonSerializer.DeserializeFromString<AppSettingsData>(serializedData);
                }
            }
        }

        public bool CanChooseProvider { get; set; }

        public bool ErrorLogEnabled
        {
            get { return false; }
        }

        public string ErrorLog
        {
            get { return ""; }
        }

        public string SiteUrl
        {
            get { return _data.SiteUrl; }
        }


        public string PhoneNumber(int providerId)
        {
            return _data.DefaultPhoneNumber;
        }
        public string PhoneNumberDisplay(int companyId)
        {
            return _data.DefaultPhoneNumberDisplay;
        }

        public int[] InvalidProviderIds
        {
            get { return new int[0]; }
        }




        public string DefaultServiceUrl
        {
            get { return "http://services.taxihail.com/{0}/v1/api/"; }

        }

        public bool CanChangeServiceUrl
        {
            get { return _data.CanChangeServiceUrl; }
        }

        public string ServiceUrl
        {
            get
            {

                var url = TinyIoC.TinyIoCContainer.Current.Resolve<ICacheService>().Get<string>("TaxiHail.ServiceUrl");
                if (string.IsNullOrEmpty(url))
                {

                    return _data.ServiceUrl;
                }
                else
                {
                    return url;
                }

            }
            set
            {
                if (CanChangeServiceUrl)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        TinyIoC.TinyIoCContainer.Current.Resolve<ICacheService>().Clear("TaxiHail.ServiceUrl");
                    }
                    else if (value.ToLower().StartsWith("http"))
                    {
                        TinyIoC.TinyIoCContainer.Current.Resolve<ICacheService>().Set<string>("TaxiHail.ServiceUrl", value);
                    }
                    else
                    {
                        TinyIoC.TinyIoCContainer.Current.Resolve<ICacheService>().Set<string>("TaxiHail.ServiceUrl", string.Format(DefaultServiceUrl, value));
                    }
                }

            }

        }


        public bool TwitterEnabled
        {
            get { return _data.TwitterEnabled; }
        }

        public string TwitterConsumerKey 
        {
            get { return _data.TwitterConsumerKey; }
        }

        public string TwitterCallback
        {
            get { return _data.TwitterCallback; }
        }

        public string TwitterConsumerSecret
        {
            get { return _data.TwitterConsumerSecret; }
        }

        public string TwitterRequestTokenUrl
        {
            get { return _data.TwitterRequestTokenUrl; }
        }

        public string TwitterAccessTokenUrl
        {
            get { return _data.TwitterAccessTokenUrl; }
        }

        public string TwitterAuthorizeUrl
        {
            get { return _data.TwitterAuthorizeUrl; }
        }

        public bool FacebookEnabled
        {
            get { return _data.FacebookEnabled; }
        }

        public string FacebookAppId
        {
            get { return _data.FacebookAppId; }
        }

        public string SupportEmail
        {
            get { return _data.SupportEmail; }
        }
        

    }
}
