﻿using System;
using System.Collections.Generic;
using System.Linq;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.Database;
using apcurium.MK.Booking.Events;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Common.Configuration.Impl;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Extensions;
using CustomerPortal.Client;
using CustomerPortal.Contract.Resources;
using CustomerPortal.Contract.Response;
using HoneyBadger;
using HoneyBadger.Responses;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;

namespace apcurium.MK.Booking.EventHandlers.Integration
{
    public class OrderDispatchCompanyManager : IIntegrationEventHandler,
        IEventHandler<OrderTimedOut>
    {
        private readonly ICommandBus _commandBus;
        private readonly Func<BookingDbContext> _contextFactory;
        private readonly IIBSServiceProvider _ibsServiceProvider;
        private readonly ITaxiHailNetworkServiceClient _taxiHailNetworkServiceClient;
        private readonly HoneyBadgerServiceClient _honeyBadgerServiceClient;

        public OrderDispatchCompanyManager(
            ICommandBus commandBus,
            Func<BookingDbContext> contextFactory,
            IIBSServiceProvider ibsServiceProvider,
            ITaxiHailNetworkServiceClient taxiHailNetworkServiceClient,
            HoneyBadgerServiceClient honeyBadgerServiceClient)
        {
            _contextFactory = contextFactory;
            _ibsServiceProvider = ibsServiceProvider;
            _commandBus = commandBus;
            _taxiHailNetworkServiceClient = taxiHailNetworkServiceClient;
            _honeyBadgerServiceClient = honeyBadgerServiceClient;
        }

        public async void Handle(OrderTimedOut @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var details = context.Find<OrderStatusDetail>(@event.SourceId);
                if (details == null)
                {
                    throw new InvalidOperationException("Order Status not found");
                }

                var order = context.Find<OrderDetail>(@event.SourceId);
                if (order == null)
                {
                    throw new InvalidOperationException("Order not found");
                }

                var pickUpPosition = new MapCoordinate
                {
                    Latitude = order.PickupAddress.Latitude,
                    Longitude = order.PickupAddress.Longitude
                };
                
                NetworkFleetResponse nextDispatchCompany;

                if (@event.Market.HasValue())
                {
                    // External market
                    var marketFleet = GetMarketFleets(@event.Market, details.CompanyKey, pickUpPosition.Latitude, pickUpPosition.Longitude);
                    nextDispatchCompany = FindNextDispatchCompany(details.CompanyKey, pickUpPosition, marketFleet, @event.Market);
                }
                else
                {
                    // Local market
                    var networkFleet = await _taxiHailNetworkServiceClient.GetNetworkFleetAsync(details.CompanyKey, pickUpPosition.Latitude, pickUpPosition.Longitude);
                    nextDispatchCompany = FindNextDispatchCompany(details.CompanyKey, pickUpPosition, networkFleet);
                }

                if (nextDispatchCompany != null)
                {
                    _commandBus.Send(new PrepareOrderForNextDispatch
                    {
                        OrderId = details.OrderId,
                        DispatchCompanyName = nextDispatchCompany.CompanyName,
                        DispatchCompanyKey = nextDispatchCompany.CompanyKey
                    });
                }
            }
        }

        private NetworkFleetResponse FindNextDispatchCompany(string currentCompanyKey, MapCoordinate pickupPosition, IList<NetworkFleetResponse> networkFleet, string market = null)
        {
            if (networkFleet == null || !networkFleet.Any() || networkFleet[0] == null)
            {
                return null;
            }

            var nextDispatchCompany = currentCompanyKey.IsNullOrEmpty()
                ? networkFleet.First()
                : FindNextAvailableCompanyInIbsZone(currentCompanyKey, pickupPosition, networkFleet, market);

            return nextDispatchCompany;
        }

        private NetworkFleetResponse FindNextAvailableCompanyInIbsZone(string currentCompanyKey, MapCoordinate pickupPosition, IList<NetworkFleetResponse> networkFleet, string market = null)
        {
            // Find the list index of the current company
            var currentDispatchCompany = networkFleet.First(f => f.CompanyKey == currentCompanyKey);
            var currentDispatchCompanyIndex = networkFleet.IndexOf(currentDispatchCompany);

            if (currentDispatchCompanyIndex == networkFleet.Count - 1)
            {
                // End of list, no more company in fleet
                return currentDispatchCompany;
            }

            var nextDispatchCompany = networkFleet[currentDispatchCompanyIndex + 1];

            // Check if company is in IBS zone
            var ibsZone = _ibsServiceProvider.StaticData(nextDispatchCompany.CompanyKey, market)
                .GetZoneByCoordinate(null, pickupPosition.Latitude, pickupPosition.Longitude);

            // If company is not IBS zone, check the next one until we have browsed the whole fleet
            return string.IsNullOrWhiteSpace(ibsZone)
                ? FindNextAvailableCompanyInIbsZone(nextDispatchCompany.CompanyKey, pickupPosition, networkFleet)
                : nextDispatchCompany;
        }

        private List<NetworkFleetResponse> GetMarketFleets(string market, string currentCompanyKey, double pickupLatitude, double pickupLongitude)
        {
            const int searchExpendLimit = 10;
            var searchRadius = 2000; // In meters

            for (var i = 1; i < searchExpendLimit; i++)
            {
                var marketVehicles =
                    _honeyBadgerServiceClient.GetAvailableVehicles(market, pickupLatitude, pickupLongitude, searchRadius, null, true)
                                             .ToArray();

                if (marketVehicles.Any())
                {
                    // Group and order fleet from max vehicle count to min
                    var orderedFleets = marketVehicles.GroupBy(v => v.FleetId)
                        .OrderBy(v => v.Count())
                        .Select(g => g.First());

                    // Fetch all NetworkFleet objects for this market except the current one
                    var marketFleets =
                        _taxiHailNetworkServiceClient.GetMarketFleets(market)
                            .Where(f => f.CompanyKey != currentCompanyKey);

                    // Return only those from the ordered fleet
                    return orderedFleets
                        .Select(v => marketFleets
                            .FirstOrDefault(f => f.FleetId == v.FleetId))
                            .ToList();
                }

                // Nothing found, extend search radius (total radius after 10 iterations: 3375m)
                searchRadius += (i * 25);
            }

            return new List<NetworkFleetResponse>();
        }
    }
}
