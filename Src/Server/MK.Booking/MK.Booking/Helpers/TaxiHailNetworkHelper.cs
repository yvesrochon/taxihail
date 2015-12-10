﻿using System;
using System.Collections.Generic;
using System.Linq;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.Data;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Booking.ReadModel.Query.Contract;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Configuration.Impl;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Enumeration;
using apcurium.MK.Common.Extensions;
using CMTServices;
using CMTServices.Responses;
using CustomerPortal.Client;
using CustomerPortal.Contract.Response;
using Infrastructure.Messaging;

namespace apcurium.MK.Booking.Helpers
{
    public class TaxiHailNetworkHelper
    {
        private readonly IAccountDao _accountDao;
        private readonly IIBSServiceProvider _ibsServiceProvider;
        private readonly IServerSettings _serverSettings;
        private readonly ITaxiHailNetworkServiceClient _taxiHailNetworkServiceClient;
        private readonly ICommandBus _commandBus;
        private readonly ILogger _logger;

        public TaxiHailNetworkHelper(
            IAccountDao accountDao,
            IIBSServiceProvider ibsServiceProvider,
            IServerSettings serverSettings,
            ITaxiHailNetworkServiceClient taxiHailNetworkServiceClient,
            ICommandBus commandBus,
            ILogger logger)
        {
            _accountDao = accountDao;
            _ibsServiceProvider = ibsServiceProvider;
            _serverSettings = serverSettings;
            _taxiHailNetworkServiceClient = taxiHailNetworkServiceClient;
            _commandBus = commandBus;
            _logger = logger;
        }

        public bool IsConfiguredForCmtPayment(string companyKey)
        {
            var paymentSettings = FetchAndSaveNetworkPaymentSettings(companyKey);

            return paymentSettings != null
                && (paymentSettings.PaymentMode == PaymentMethod.Cmt
                    || paymentSettings.PaymentMode == PaymentMethod.RideLinqCmt);
        }

        public ServerPaymentSettings FetchAndSaveNetworkPaymentSettings(string companyKey)
        {
            try
            {
                var paymentSettings = _serverSettings.GetPaymentSettings();
                var companyPaymentSettings = _taxiHailNetworkServiceClient.GetPaymentSettings(companyKey);

                // Mobile will always keep local settings. The only values that needs to be overridden are the payment providers settings.
                paymentSettings.Id = Guid.NewGuid();
                paymentSettings.CompanyKey = companyKey;
                paymentSettings.PaymentMode = companyPaymentSettings.PaymentMode;
                paymentSettings.BraintreeServerSettings = new BraintreeServerSettings
                {
                    IsSandbox = companyPaymentSettings.BraintreePaymentSettings.IsSandbox,
                    MerchantId = companyPaymentSettings.BraintreePaymentSettings.MerchantId,
                    PrivateKey = companyPaymentSettings.BraintreePaymentSettings.PrivateKey,
                    PublicKey = companyPaymentSettings.BraintreePaymentSettings.PublicKey
                };
                paymentSettings.BraintreeClientSettings = new BraintreeClientSettings
                {
                    ClientKey = companyPaymentSettings.BraintreePaymentSettings.ClientKey
                };
                paymentSettings.MonerisPaymentSettings = new MonerisPaymentSettings
                {
                    IsSandbox = companyPaymentSettings.MonerisPaymentSettings.IsSandbox,
                    ApiToken = companyPaymentSettings.MonerisPaymentSettings.ApiToken,
                    BaseHost = companyPaymentSettings.MonerisPaymentSettings.BaseHost,
                    SandboxHost = companyPaymentSettings.MonerisPaymentSettings.SandboxHost,
                    StoreId = companyPaymentSettings.MonerisPaymentSettings.StoreId
                };
                paymentSettings.CmtPaymentSettings = new CmtPaymentSettings
                {
                    BaseUrl = companyPaymentSettings.CmtPaymentSettings.BaseUrl,
                    ConsumerKey = companyPaymentSettings.CmtPaymentSettings.ConsumerKey,
                    ConsumerSecretKey = companyPaymentSettings.CmtPaymentSettings.ConsumerSecretKey,
                    CurrencyCode = companyPaymentSettings.CmtPaymentSettings.CurrencyCode,
                    FleetToken = companyPaymentSettings.CmtPaymentSettings.FleetToken,
                    IsManualRidelinqCheckInEnabled = companyPaymentSettings.CmtPaymentSettings.IsManualRidelinqCheckInEnabled,
                    IsSandbox = companyPaymentSettings.CmtPaymentSettings.IsSandbox,
                    Market = companyPaymentSettings.CmtPaymentSettings.Market,
                    MobileBaseUrl = companyPaymentSettings.CmtPaymentSettings.MobileBaseUrl,
                    SandboxBaseUrl = companyPaymentSettings.CmtPaymentSettings.SandboxBaseUrl,
                    SandboxMobileBaseUrl = companyPaymentSettings.CmtPaymentSettings.SandboxMobileBaseUrl
                };

                // Save/update network company settings
                _commandBus.Send(new SaveTemporaryCompanyPaymentSettings
                {
                    ServerPaymentSettings = paymentSettings
                });

                return paymentSettings;
            }
            catch (Exception ex)
            {
                _logger.LogMessage(string.Format("An error occurred when trying to get PaymentSettings for company {0}", companyKey));
                _logger.LogError(ex);

                return null;
            }
        }

        public void UpdateVehicleTypeFromMarketData(BookingSettings bookingSettings, string marketCompanyId)
        {
            if (!bookingSettings.VehicleTypeId.HasValue || !marketCompanyId.HasValue())
            {
                // Nothing to do
                return;
            }

            try
            {
                // Get the vehicle type defined for the market of the company
                var matchingMarketVehicle = _taxiHailNetworkServiceClient.GetAssociatedMarketVehicleType(marketCompanyId, bookingSettings.VehicleTypeId.Value);
                if (matchingMarketVehicle != null)
                {
                    // Update the vehicle type info using the vehicle id from the IBS of that company
                    bookingSettings.VehicleType = matchingMarketVehicle.Name;
                    bookingSettings.VehicleTypeId = matchingMarketVehicle.ReferenceDataVehicleId;
                }
                else
                {
                    // No match found
                    bookingSettings.VehicleType = null;
                    bookingSettings.VehicleTypeId = null;

                    _logger.LogMessage(string.Format("No match found for GetAssociatedMarketVehicleType for company {0}. Maybe no vehicles were linked via the admin panel?", marketCompanyId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogMessage(string.Format("An error occurred when trying to get GetAssociatedMarketVehicleType for company {0}", marketCompanyId));
                _logger.LogError(ex);
            }
        }

        public BestAvailableCompany FindSpecificCompany(string market, CreateReportOrder createReportOrder, string orderCompanyKey = null, int? orderFleetId = null, double? latitude = null, double? longitude = null)
        {
            if (!orderCompanyKey.HasValue() && !orderFleetId.HasValue)
            {
                Exception createOrderException = new ArgumentNullException("You must at least provide a value for orderCompanyKey or orderFleetId");
                createReportOrder.Error = createOrderException.ToString();
                _commandBus.Send(createReportOrder);
                throw createOrderException;
            }

            var companyKey = _serverSettings.ServerData.TaxiHail.ApplicationKey;

            var fleets = market.HasValue()
                ? _taxiHailNetworkServiceClient.GetMarketFleets(companyKey, market).ToArray()
                : _taxiHailNetworkServiceClient.GetNetworkFleet(companyKey, latitude, longitude).ToArray();

            if (orderCompanyKey.HasValue())
            {
                var match = fleets.FirstOrDefault(f => f.CompanyKey == orderCompanyKey);
                if (match != null)
                {
                    return new BestAvailableCompany
                    {
                        CompanyKey = match.CompanyKey,
                        CompanyName = match.CompanyName,
                        FleetId = match.FleetId
                    };
                }
            }

            if (orderFleetId.HasValue)
            {
                var match = fleets.FirstOrDefault(f => f.FleetId == orderFleetId.Value);
                if (match != null)
                {
                    return new BestAvailableCompany
                    {
                        CompanyKey = match.CompanyKey,
                        CompanyName = match.CompanyName,
                        FleetId = match.FleetId
                    };
                }
            }

            // Nothing found
            return new BestAvailableCompany();
        }

        public BestAvailableCompany FindBestAvailableCompany(string market, double? latitude, double? longitude, List<string> driverIdsToExclude = null)
        {
            if (!market.HasValue() || !latitude.HasValue || !longitude.HasValue)
            {
                // Do nothing if in home market or if we don't have position
                _logger.LogMessage("FindBestAvailableCompany - We are in local market (or lat/lng is null), skip honeybadger/geo and call local ibs first");
                return new BestAvailableCompany();
            }

            int? bestFleetId = null;
            const int searchExpendLimit = 10;
            var searchRadius = 2000; // In meters

            var marketFleets = _taxiHailNetworkServiceClient.GetMarketFleets(null, market).ToArray();
            var marketFleetIds = marketFleets.Select(x => x.FleetId).Distinct().ToArray();
            for (var i = 1; i < searchExpendLimit; i++)
            {
                var marketVehicles = GetAvailableVehiclesServiceClient(market)
                    .GetAvailableVehicles(market, latitude.Value, longitude.Value, searchRadius, marketFleetIds, true)
                    .ToArray();

                var marketVehiclesFilteredIfNecessary = driverIdsToExclude != null
                    ? marketVehicles
                        .Where(x => !driverIdsToExclude.Contains(x.DeviceName)
                                 && !driverIdsToExclude.Contains(x.LegacyDispatchId))
                        .ToArray()
                    : marketVehicles;

                if (marketVehiclesFilteredIfNecessary.Any())
                {
                    bestFleetId = marketVehiclesFilteredIfNecessary
                        .OrderBy(vehicle => vehicle.Eta.HasValue)
                        .ThenBy(vehicle => vehicle.Eta)
                        .First()
                        .FleetId;

                    break;
                }

                // Nothing found, extend search radius (total radius after 10 iterations: 3375m)
                searchRadius += (i * 25);
            }
            
            // Nothing found
            if (!bestFleetId.HasValue)
            {
                return new BestAvailableCompany();
            }

            // Fallback: If for some reason, we cannot find a match for the best fleet id in the fleets
            // that were setup for the market, we take the first one
            var bestFleet = marketFleets.FirstOrDefault(f => f.FleetId == bestFleetId.Value)
                            ?? marketFleets.FirstOrDefault();
            
            // Nothing found
            if (bestFleet == null)
            {
                return new BestAvailableCompany();
            }

            return new BestAvailableCompany
            {
                CompanyKey = bestFleet.CompanyKey,
                CompanyName = bestFleet.CompanyName,
                FleetId = bestFleet.FleetId
            };
        }

        public int CreateIbsAccountIfNeeded(AccountDetail account, string companyKey = null)
        {
            var ibsAccountId = _accountDao.GetIbsAccountId(account.Id, companyKey);
            if (ibsAccountId.HasValue)
            {
                return ibsAccountId.Value;
            }

            // Account doesn't exist, create it
            ibsAccountId = _ibsServiceProvider.Account(companyKey).CreateAccount(account.Id,
                account.Email,
                string.Empty,
                account.Name,
                account.Settings.Phone);

            _commandBus.Send(new LinkAccountToIbs
            {
                AccountId = account.Id,
                IbsAccountId = ibsAccountId.Value,
                CompanyKey = companyKey
            });

            return ibsAccountId.Value;
        }

        public BaseAvailableVehicleServiceClient GetAvailableVehiclesServiceClient(string market)
        {
            if (IsCmtGeoServiceMode(market))
            {
                return new CmtGeoServiceClient(_serverSettings, _logger);
            }

            return new HoneyBadgerServiceClient(_serverSettings, _logger);
        }

        private bool IsCmtGeoServiceMode(string market)
        {
            var externalMarketMode = market.HasValue() && _serverSettings.ServerData.ExternalAvailableVehiclesMode == ExternalAvailableVehiclesModes.Geo;

            var internalMarketMode = !market.HasValue() && _serverSettings.ServerData.LocalAvailableVehiclesMode == LocalAvailableVehiclesModes.Geo;

            return internalMarketMode || externalMarketMode;
        }
    }
}