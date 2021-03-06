﻿#region

using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Common.Entity;

#endregion

namespace apcurium.MK.Booking.Api.Services
{
    public class OrderMapper
    {
        public Order ToResource(OrderDetail order)
        {
            var resource = new Order
            {
                Id = order.Id,
                PickupDate = order.PickupDate,
                CreatedDate = order.CreatedDate,
                IBSOrderId = order.IBSOrderId,
                PickupAddress = order.PickupAddress,
                DropOffAddress = order.DropOffAddress,
                Settings = new BookingSettings
                {
                    ChargeTypeId = order.Settings.ChargeTypeId,
                    Name = order.Settings.Name,
                    Phone = order.Settings.Phone,
                    NumberOfTaxi = 1,
                    Passengers = order.Settings.Passengers,
                    ProviderId = order.Settings.ProviderId,
                    VehicleTypeId = order.Settings.VehicleTypeId,
                    LargeBags = order.Settings.LargeBags,
                    AccountNumber = order.Settings.AccountNumber,
                    CustomerNumber = order.Settings.CustomerNumber
                },
                Note = order.UserNote,
                Fare = order.Fare,
                Tax = order.Tax,
                Tip = order.Tip,
                Toll = order.Toll,
                Surcharge = order.Surcharge,
                Status = (OrderStatus) order.Status,
                IsRated = order.IsRated,
                TransactionId = order.TransactionId,
                IsManualRideLinq = order.IsManualRideLinq
            };

            return resource;
        }

    }
}