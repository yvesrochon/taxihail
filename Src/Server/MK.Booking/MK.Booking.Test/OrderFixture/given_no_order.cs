﻿#region

using System;
using apcurium.MK.Booking.CommandHandlers;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.Database;
using apcurium.MK.Booking.Domain;
using apcurium.MK.Booking.Events;
using apcurium.MK.Booking.Test.Integration;
using apcurium.MK.Common.Entity;
using NUnit.Framework;

#endregion

namespace apcurium.MK.Booking.Test.OrderFixture
{
    [TestFixture]
    public class given_no_order : given_a_read_model_database
    {
        [SetUp]
        public void Setup()
        {
            _sut = new EventSourcingTestHelper<Order>();
            _sut.Setup(new OrderCommandHandler(_sut.Repository, () => new BookingDbContext(DbName)));
            _sut.Given(new AccountRegistered
            {
                SourceId = _accountId,
                Name = "Bob",
                Password = null,
                Email = "bob.smith@apcurium.com"
            });
        }

        private EventSourcingTestHelper<Order> _sut;
        private readonly Guid _accountId = Guid.NewGuid();

        [Test]
        public void when_creating_an_order_successfully()
        {
            var pickupDate = DateTime.Now;
            var order = new CreateOrder
            {
                AccountId = _accountId,
                PickupDate = pickupDate,
                PickupAddress =
                    new Address
                    {
                        RingCode = "3131",
                        Latitude = 45.515065,
                        Longitude = -73.558064,
                        FullAddress = "1234 rue Saint-Hubert",
                        Apartment = "3939"
                    },
                DropOffAddress =
                    new Address {Latitude = 45.50643, Longitude = -73.554052, FullAddress = "Velvet auberge st gabriel"},
                ClientLanguageCode = "fr",
                UserLatitude = 46.50643,
                UserLongitude = -74.554052,
                UserAgent = "TestUserAgent",
                ClientVersion = "1.0.0",
                UserNote = "une note"
            };
            order.Settings = new BookingSettings
            {
                ChargeTypeId = 99,
                VehicleTypeId = 88,
                ProviderId = 11,
                Phone = "514-555-1212",
                Passengers = 6,
                NumberOfTaxi = 1,
                Name = "Joe Smith"
            };

            _sut.When(order);

            var orderCreated = _sut.ThenHasSingle<OrderCreated>();
            Assert.AreEqual(_accountId, orderCreated.AccountId);
            Assert.AreEqual(pickupDate, orderCreated.PickupDate);
            Assert.AreEqual("3939", orderCreated.PickupAddress.Apartment);
            Assert.AreEqual("1234 rue Saint-Hubert", orderCreated.PickupAddress.FullAddress);
            Assert.AreEqual("3131", orderCreated.PickupAddress.RingCode);
            Assert.AreEqual(45.515065, orderCreated.PickupAddress.Latitude);
            Assert.AreEqual(-73.558064, orderCreated.PickupAddress.Longitude);
            Assert.AreEqual("Velvet auberge st gabriel", orderCreated.DropOffAddress.FullAddress);
            Assert.AreEqual(45.50643, orderCreated.DropOffAddress.Latitude);
            Assert.AreEqual(-73.554052, orderCreated.DropOffAddress.Longitude);
            Assert.AreEqual(99, orderCreated.Settings.ChargeTypeId);
            Assert.AreEqual(88, orderCreated.Settings.VehicleTypeId);
            Assert.AreEqual(11, orderCreated.Settings.ProviderId);
            Assert.AreEqual("514-555-1212", orderCreated.Settings.Phone);
            Assert.AreEqual(6, orderCreated.Settings.Passengers);
            Assert.AreEqual(1, orderCreated.Settings.NumberOfTaxi);
            Assert.AreEqual("Joe Smith", orderCreated.Settings.Name);
            Assert.AreEqual("fr", orderCreated.ClientLanguageCode);
            Assert.AreEqual(46.50643, orderCreated.UserLatitude);
            Assert.AreEqual(-74.554052, orderCreated.UserLongitude);
            Assert.AreEqual("TestUserAgent", orderCreated.UserAgent);
            Assert.AreEqual("1.0.0", orderCreated.ClientVersion);
            Assert.AreEqual("une note", orderCreated.UserNote);
        }

        [Test]
        public void when_creating_an_order_with_payment_information()
        {
            var pickupDate = DateTime.Now;
            var creditCardId = Guid.NewGuid();
            var order = new CreateOrder
            {
                AccountId = _accountId,
                PickupDate = pickupDate,
                PickupAddress =
                    new Address
                    {
                        RingCode = "3131",
                        Latitude = 45.515065,
                        Longitude = -73.558064,
                        FullAddress = "1234 rue Saint-Hubert",
                        Apartment = "3939"
                    },
                DropOffAddress =
                    new Address {Latitude = 45.50643, Longitude = -73.554052, FullAddress = "Velvet auberge st gabriel"},
            };
            order.Settings = new BookingSettings
            {
                ChargeTypeId = 99,
                VehicleTypeId = 88,
                ProviderId = 11,
                Phone = "514-555-1212",
                Passengers = 6,
                NumberOfTaxi = 1,
                Name = "Joe Smith"
            };
            order.Payment = new CreateOrder.PaymentInformation
            {
                PayWithCreditCard = true,
                CreditCardId = creditCardId,
                TipPercent = 15
            };

            _sut.When(order);

            Assert.AreEqual(2, _sut.Events.Count);

            var paymentInformationSet = (PaymentInformationSet) _sut.Events[1];
            Assert.AreEqual(creditCardId, paymentInformationSet.CreditCardId);
            Assert.AreEqual(15, paymentInformationSet.TipPercent);
            Assert.AreEqual(null, paymentInformationSet.TipAmount);
        }

        [Test]
        public void when_creating_an_order_without_dropOff_successfully()
        {
            var pickupDate = DateTime.Now;

            var order = new CreateOrder
            {
                AccountId = _accountId,
                PickupDate = pickupDate,
                PickupAddress =
                    new Address
                    {
                        RingCode = "3131",
                        Latitude = 45.515065,
                        Longitude = -73.558064,
                        FullAddress = "1234 rue Saint-Hubert",
                        Apartment = "3939"
                    },
            };
            order.Settings = new BookingSettings
            {
                ChargeTypeId = 99,
                VehicleTypeId = 88,
                ProviderId = 11,
                Phone = "514-555-1212",
                Passengers = 6,
                NumberOfTaxi = 1,
                Name = "Joe Smith"
            };

            _sut.When(order);

            var orderCreated = _sut.ThenHasSingle<OrderCreated>();
            Assert.AreEqual(_accountId, orderCreated.AccountId);
            Assert.AreEqual(pickupDate, orderCreated.PickupDate);
            Assert.AreEqual("3939", orderCreated.PickupAddress.Apartment);
            Assert.AreEqual("1234 rue Saint-Hubert", orderCreated.PickupAddress.FullAddress);
            Assert.AreEqual("3131", orderCreated.PickupAddress.RingCode);
            Assert.AreEqual(45.515065, orderCreated.PickupAddress.Latitude);
            Assert.AreEqual(-73.558064, orderCreated.PickupAddress.Longitude);
            Assert.IsNull(orderCreated.DropOffAddress);
        }
    }
}