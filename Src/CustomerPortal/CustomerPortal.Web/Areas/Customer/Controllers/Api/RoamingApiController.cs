﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CustomerPortal.Contract.Resources;
using CustomerPortal.Contract.Response;
using CustomerPortal.Web.Entities;
using CustomerPortal.Web.Entities.Network;
using CustomerPortal.Web.Extensions;
using MongoRepository;
using Newtonsoft.Json;

namespace CustomerPortal.Web.Areas.Customer.Controllers.Api
{
    public class RoamingApiController : ApiController
    {
        private readonly IRepository<TaxiHailNetworkSettings> _networkRepository;
        private readonly IRepository<Company> _companyRepository;


        public RoamingApiController() 
            : this(new MongoRepository<TaxiHailNetworkSettings>(), new MongoRepository<Company>())
        {
        }

        public RoamingApiController(IRepository<TaxiHailNetworkSettings> networkRepository, IRepository<Company> companyRepository)
        {
            _networkRepository = networkRepository;
            _companyRepository = companyRepository;
        }

        [Route("api/customer/{companyId}/roaming/networkfleets")]
        public HttpResponseMessage GetRoamingFleetsPreferences(string companyId)
        {
            var homeCompanySettings = _networkRepository.FirstOrDefault(n => n.Id == companyId);

            if (homeCompanySettings == null || !homeCompanySettings.IsInNetwork)
            {
                return null;
            }

            var companiesFromOtherMarkets = _networkRepository
                .Where(n => n.IsInNetwork
                    && n.Id != homeCompanySettings.Id
                    && n.Market != homeCompanySettings.Market);

            var preferences = new Dictionary<string, List<CompanyPreferenceResponse>>();

            foreach (var roamingCompany in companiesFromOtherMarkets)
            {
                var companyPreference = homeCompanySettings.Preferences.FirstOrDefault(p => p.CompanyKey == roamingCompany.Id)
                            ?? new CompanyPreference { CompanyKey = roamingCompany.Id };

                var roamingCompanyAllowUsToDispatch = roamingCompany.Preferences.Any(x => x.CompanyKey == companyId && x.CanAccept);

                if (!preferences.ContainsKey(roamingCompany.Market))
                {
                    preferences.Add(roamingCompany.Market, new List<CompanyPreferenceResponse>());
                }

                preferences[roamingCompany.Market].Add(new CompanyPreferenceResponse
                {
                    CompanyPreference = companyPreference,
                    CanDispatchTo = roamingCompanyAllowUsToDispatch
                });
            }

            var markets = new List<string>(preferences.Keys);
            foreach (var market in markets)
            {
                preferences[market] = preferences[market]
                    .OrderBy(p => p.CompanyPreference.Order == null)
                    .ThenBy(p => p.CompanyPreference.Order)
                    .ToList();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(preferences))
            };
        }

        [Route("api/customer/roaming/market")]
        public HttpResponseMessage GetCompanyMarket(string companyId, double latitude, double longitude)
        {
            string companyMarket = null;

            var userPosition = new MapCoordinate
            {
                Latitude = latitude,
                Longitude = longitude
            };

            // Get all companies in network
            var companiesInNetwork = _networkRepository.Where(n => n.IsInNetwork).ToArray();

            // Find the first company that includes the user position
            // (it doesn't matter which one because they will all share the same market key anyway)
            var localCompany = companiesInNetwork.FirstOrDefault(x => x.Region.Contains(userPosition));
            if (localCompany != null)
            {
                // Check if we have changed market
                var homeCompany = _networkRepository.FirstOrDefault(n => n.Id == companyId);
                if (homeCompany != null)
                {
                    if (localCompany.Market != homeCompany.Market)
                    {
                        companyMarket = localCompany.Market;
                    }
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(companyMarket)) 
            };
        }

        [Route("api/customer/{companyId}/roaming/marketfleets")]
        public HttpResponseMessage GetMarketFleets(string companyId, string market)
        {
            var marketFleets = new List<NetworkFleetResponse>();

            // Get all companies in the market that accepts dispatch for the company
            var companiesInMarket = _networkRepository.Where(n => n.IsInNetwork
                && n.Market == market
                && n.Preferences.Any(p => p.CompanyKey == companyId && p.CanAccept));

            foreach (var availableCompany in companiesInMarket)
            {
                var company = _companyRepository.FirstOrDefault(c => c.CompanyKey == availableCompany.Id);
                if (company != null)
                {
                    marketFleets.Add(new NetworkFleetResponse
                    {
                        CompanyKey = company.CompanyKey,
                        CompanyName = company.CompanyName,
                        FleetId = availableCompany.FleetId,
                        IbsPassword = company.IBS.Password,
                        IbsUserName = company.IBS.Username,
                        IbsUrl = company.IBS.ServiceUrl,
                        IbsTimeDifference = GetIbsTimeDifference(company)
                    });
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(marketFleets))
            };
        }

        [Route("api/customer/roaming/marketfleet")]
        public HttpResponseMessage GetMarketFleet(string market, int fleetId)
        {
            // Get all companies in the market
            var companiesInMarket = _networkRepository.Where(n => n.IsInNetwork && n.Market == market);
            var fleet = companiesInMarket.FirstOrDefault(c => c.FleetId == fleetId);

            if (fleet != null)
            {
                var company = _companyRepository.FirstOrDefault(c => c.CompanyKey == fleet.Id);
                if (company != null)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(
                            new NetworkFleetResponse
                            {
                                CompanyKey = company.CompanyKey,
                                CompanyName = company.CompanyName,
                                FleetId = fleet.FleetId,
                                IbsPassword = company.IBS.Password,
                                IbsUserName = company.IBS.Username,
                                IbsUrl = company.IBS.ServiceUrl,
                                IbsTimeDifference = GetIbsTimeDifference(company)
                            }))
                    };
                }                
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(null))
            };
        }

        private long GetIbsTimeDifference(Company company)
        {
            long ibsTimeDifference = 0;

            var ibsTimeDifferenceString = company.CompanySettings.FirstOrDefault(s => s.Key == "IBS.TimeDifference");
            if (ibsTimeDifferenceString != null)
            {
                long.TryParse(ibsTimeDifferenceString.Value, out ibsTimeDifference);
            }

            return ibsTimeDifference;
        }
    }
}