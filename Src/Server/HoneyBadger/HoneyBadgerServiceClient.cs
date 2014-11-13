﻿using System;
using System.Collections.Generic;
using System.Linq;
using HoneyBadger.Enums;
using HoneyBadger.Extensions;
using HoneyBadger.Responses;

namespace HoneyBadger
{
    public class HoneyBadgerServiceClient : BaseServiceClient
    {
        public IEnumerable<VehicleResponse> GetAvailableVehicles(string market, string fleetId)
        {
            var @params = new Dictionary<string, string>
                {
                    { "market", market },
                    { "fleet", fleetId },
                    { "includeEntities", "true" },
                    { "meterState", ((int)MeterStates.ForHire).ToString() }
                };

            var queryString = BuildQueryString(@params);

            var response = Client.Get("availability" + queryString)
                                 .Deserialize<HoneyBadgerResponse>()
                                 .Result;

            return response.Entities.Select(e => new VehicleResponse
            {
                Timestamp = e.TimeStamp,
                Latitude = e.Latitude,
                Longitude = e.Longitude
            });
        }

        public IEnumerable<VehicleResponse> GetAvailableVehicles(string market, IEnumerable<string> fleetId)
        {
            // Waiting on MK for this one.
            throw new NotImplementedException();
        }
    }
}