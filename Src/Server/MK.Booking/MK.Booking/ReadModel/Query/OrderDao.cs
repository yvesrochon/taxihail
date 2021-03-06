﻿#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using apcurium.MK.Booking.Database;
using apcurium.MK.Booking.Maps.Geo;
using apcurium.MK.Booking.PushNotifications;
using apcurium.MK.Booking.ReadModel.Query.Contract;
using apcurium.MK.Common;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Extensions;
using AutoMapper;

#endregion

namespace apcurium.MK.Booking.ReadModel.Query
{
    public class OrderDao : IOrderDao
    {
        private readonly Func<BookingDbContext> _contextFactory;
        private readonly IServerSettings _serverSettings;

        public OrderDao(Func<BookingDbContext> contextFactory, IServerSettings serverSettings)
        {
            _contextFactory = contextFactory;
            _serverSettings = serverSettings;
        }

        public IList<OrderDetail> GetAll()
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<OrderDetail>().ToList();
            }
        }

        public OrderDetail FindById(Guid id)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<OrderDetail>().SingleOrDefault(c => c.Id == id);
            }
        }

        public IList<OrderDetail> FindByAccountId(Guid id)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<OrderDetail>().Where(c => c.AccountId == id).ToList();
            }
        }

        public IList<OrderStatusDetail> FindOrderStatusByAccountId(Guid accountId)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<OrderStatusDetail>().Where(c => c.AccountId == accountId).ToList();
            }
        }
        
        /// <summary>
        /// Returns all active orders (active in terms of the UpdateOrderStatusJob)
        /// </summary>
        /// <param name="forManualRideLinq"></param>
        /// <returns></returns>
        public IList<OrderStatusDetail> GetOrdersInProgress(bool forManualRideLinq)
        {
            using (var context = _contextFactory.Invoke())
            {
                if (forManualRideLinq)
                {
                    return context.Set<OrderStatusDetail>()
                        .Where(x => (x.Status == OrderStatus.Created
                                  || x.Status == OrderStatus.WaitingForPayment)
                               && x.IsManualRideLinq)
                        .ToList();
                }

                // minimize the number of orders that are considered in progress by setting a floor and ceiling
                var startDate = DateTime.Now.AddHours(0 - _serverSettings.ServerData.OrderStatus.PickupDateNoOlderThan);
                var endDate = DateTime.Now.AddHours(_serverSettings.ServerData.OrderStatus.PickupDateNoNewerThan);

                return context.Set<OrderStatusDetail>()
                    .Where(x => (x.Status == OrderStatus.Created
                                 || x.Status == OrderStatus.Pending
                                 || x.Status == OrderStatus.WaitingForPayment
                                 || x.Status == OrderStatus.TimedOut)
                                && x.PickupDate >= startDate
                                && (x.PickupDate <= endDate || String.IsNullOrEmpty(x.IBSStatusId))
                                && !x.IsManualRideLinq)
                    .ToList();
            }
        }

        public OrderStatusDetail GetActiveOrderStatusDetails(Guid accountId)
        {
            var activeOrder = GetOrdersInProgressByAccountId(accountId)
                .Where(orderStatusDetail => VehicleStatuses.OrderActiveStatuses.Any(status => status == orderStatusDetail.IBSStatusId) || orderStatusDetail.IsManualRideLinq)
                .OrderByDescending(order => order.PickupDate)
                .FirstOrDefault();

            if (activeOrder == null || !activeOrder.IsManualRideLinq)
            {
                return activeOrder;
            }

            // Verifying if the manual paired order is still active.
            var manualPairingDetails = GetManualRideLinqById(activeOrder.OrderId);

            return manualPairingDetails.EndTime.HasValue
                ? null
                : activeOrder;
        }

        public IList<OrderStatusDetail> GetOrdersInProgressByAccountId(Guid accountId)
        {
            using (var context = _contextFactory.Invoke())
            {
                var startDate = DateTime.Now.AddHours(-36);

                var currentOrders = (from order in context.Set<OrderStatusDetail>()
                    where order.AccountId == accountId
                    where (order.Status == OrderStatus.Created
                        || order.Status == OrderStatus.Pending)
                        && (order.PickupDate >= startDate)
                    select order).ToList();
                return currentOrders;
            }
        }

        public OrderStatusDetail FindOrderStatusById(Guid orderId)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<OrderStatusDetail>().SingleOrDefault(x => x.OrderId == orderId);
            }
        }

        public OrderPairingDetail FindOrderPairingById(Guid orderId)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<OrderPairingDetail>().SingleOrDefault(x => x.OrderId == orderId);
            }
        }

        public void UpdateVehiclePosition(Guid orderId, double? newLatitude, double? newLongitude)
        {
            using (var context = _contextFactory.Invoke())
            {
                var orderStatus = context.Query<OrderStatusDetail>().Single(x => x.OrderId == orderId);

                orderStatus.VehicleLatitude = newLatitude;
                orderStatus.VehicleLongitude = newLongitude;

                context.Save(orderStatus);

                if (VehicleStatuses.LogVehiclePositionForOrderStatuses.Any(s => s.SoftEqual(orderStatus.IBSStatusId))
                    && newLatitude.HasValue 
                    && newLongitude.HasValue)
                {
                    context.Save(new OrderVehiclePositionDetail
                    {
                        OrderId = orderId,
                        Latitude = newLatitude.Value,
                        Longitude = newLongitude.Value,
                        DateOfPosition = DateTime.UtcNow
                    });
                }
            }
        }

        public IEnumerable<Position> GetVehiclePositions(Guid orderId)
        {
            using (var context = _contextFactory.Invoke())
            {
                var vehiclePositions = context.Query<OrderVehiclePositionDetail>().Where(x => x.OrderId == orderId);
                return vehiclePositions
                    .OrderBy(x => x.DateOfPosition)
                    .ToList()
                    .Select(x => new Position(x.Latitude, x.Longitude));
            }
        }

        public TemporaryOrderCreationInfoDetail GetTemporaryInfo(Guid orderId)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<TemporaryOrderCreationInfoDetail>().SingleOrDefault(c => c.OrderId == orderId);
            }
        }

        public TemporaryOrderPaymentInfoDetail GetTemporaryPaymentInfo(Guid orderId)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<TemporaryOrderPaymentInfoDetail>().SingleOrDefault(c => c.OrderId == orderId);
            }
        }

        public void DeleteTemporaryPaymentInfo(Guid orderId)
        {
            using (var context = _contextFactory.Invoke())
            {
                context.RemoveWhere<TemporaryOrderPaymentInfoDetail>(c => c.OrderId == orderId);
                context.SaveChanges();
            }
        }

        public OrderManualRideLinqDetail GetManualRideLinqById(Guid orderId)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<OrderManualRideLinqDetail>().SingleOrDefault(c => c.OrderId == orderId);
            }
        }

	    public OrderManualRideLinqDetail GetCurrentManualRideLinq(string pairingCode, Guid accountId)
        {
            using (var context = _contextFactory.Invoke())
            {
                var today = DateTime.Now.Date;
                return context
                    .Query<OrderManualRideLinqDetail>()
                    .Where(ridelinq => ridelinq.PairingCode.Equals(pairingCode)
                        && ridelinq.AccountId == accountId)
                    .ToList()
                    .Where(ridelinq => ridelinq.StartTime.HasValue
                        && ridelinq.StartTime.Value.Date.Year == today.Year
                        && ridelinq.StartTime.Value.Date.Month == today.Month
                        && ridelinq.StartTime.Value.Date.Day == today.Day)
                    .OrderBy(ridelinq => ridelinq.StartTime)
                    .LastOrDefault();

            }
        }
    }
}