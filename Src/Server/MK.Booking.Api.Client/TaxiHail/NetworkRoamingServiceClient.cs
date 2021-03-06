﻿using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using apcurium.MK.Common;
using apcurium.MK.Common.Diagnostic;

#if !CLIENT
using apcurium.MK.Booking.Api.Client.Extensions;
#endif
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Common.Extensions;

namespace apcurium.MK.Booking.Api.Client.TaxiHail
{
    public class NetworkRoamingServiceClient : BaseServiceClient
    {
        public NetworkRoamingServiceClient(string url, string sessionId, IPackageInfo packageInfo, IConnectivityService connectivityService, ILogger logger)
            : base(url, sessionId, packageInfo, connectivityService, logger)
        {
        }

        public Task<string> GetHashedCompanyMarket(double latitude, double longitude)
        {
            var @params = new Dictionary<string, string>
                {
                    {"latitude", latitude.ToString(CultureInfo.InvariantCulture) },
                    {"longitude", longitude.ToString(CultureInfo.InvariantCulture) }
                };

            var queryString = BuildQueryString(@params);

            return Client.GetAsync<string>("/roaming/market" + queryString, logger: Logger);
        }

        public Task<MarketSettings> GetCompanyMarketSettings(double latitude, double longitude)
        {
            var @params = new Dictionary<string, string>
                {
                    {"latitude", latitude.ToString(CultureInfo.InvariantCulture) },
                    {"longitude", longitude.ToString(CultureInfo.InvariantCulture) }
                };

            var queryString = BuildQueryString(@params);

            return Client.GetAsync<MarketSettings>("/roaming/marketsettings" + queryString, logger: Logger);
        }

        public Task<List<NetworkFleet>> GetNetworkFleets()
        {
            return Client.GetAsync<List<NetworkFleet>>("/network/networkfleets", logger: Logger);
        }

        public Task<List<VehicleType>> GetExternalMarketVehicleTypes(double latitude, double longitude)
        {
            var @params = new Dictionary<string, string>
                {
                    {"latitude", latitude.ToString(CultureInfo.InvariantCulture) },
                    {"longitude", longitude.ToString(CultureInfo.InvariantCulture) }
                };

            var queryString = BuildQueryString(@params);

            return Client.GetAsync<List<VehicleType>>("/roaming/externalMarketVehicleTypes" + queryString, logger: Logger);
        }
    }
}
