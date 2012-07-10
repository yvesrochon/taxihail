﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using apcurium.MK.Booking.Api.Client;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.ReadModel.Query;

namespace apcurium.MK.Web.Tests
{
    public class OrderFixture : BaseTest
    {
        private readonly Guid _orderId = Guid.NewGuid();
        [TestFixtureSetUp]
        public new void Setup()
        {
            base.Setup();
            var sut = new OrderServiceClient(BaseUrl, new AuthInfo(TestAccount.Email, TestAccountPassword));
            var order = new CreateOrder
            {
                Id = _orderId,
                AccountId = TestAccount.Id,
                PickupApartment = "3939",
                PickupAddress = "1234 rue Saint-Hubert",
                PickupRingCode = "3131",
                PickupLatitude = 45.515065,
                PickupLongitude = -73.558064,
                PickupDate = DateTime.Now,
                DropOffAddress = "Velvet auberge st gabriel",
                DropOffLatitude = 45.50643,
                DropOffLongitude = -73.554052
            };
            sut.CreateOrder(order);
        }

        [TestFixtureTearDown]
        public new void TearDown()
        {
            base.TearDown();
        }

        [SetUp]
        public void SetupTest()
        {
         
        }

        [Test]
        public void CreateOrder()
        {
            var sut = new OrderServiceClient(BaseUrl, new AuthInfo(TestAccount.Email, TestAccountPassword));
            var pickupDate = DateTime.Now;
            var requestDate = DateTime.Now.AddHours(1);
            var order = new CreateOrder
                            {
                                Id = Guid.NewGuid(),
                                AccountId = TestAccount.Id,                                
                                PickupApartment = "3939",
                                PickupAddress = "1234 rue Saint-Hubert",
                                PickupRingCode = "3131",
                                PickupLatitude = 45.515065,
                                PickupLongitude = -73.558064,
                                PickupDate = pickupDate,
                                DropOffAddress = "Velvet auberge st gabriel",
                                DropOffLatitude = 45.50643,
                                DropOffLongitude = -73.554052
                            };

            var id = sut.CreateOrder(order);
            Assert.NotNull(id);
        }

        [Test]
        public void GetOrderList()
        {
            var sut = new OrderServiceClient(BaseUrl, new AuthInfo(TestAccount.Email, TestAccountPassword));
            
            var orders = sut.GetOrdersByAccount(new AccountOrderListRequest()
            {
                AccountId = TestAccount.Id
            });
            Assert.NotNull(orders);
        }

        [Test]
        public void GetOrder()
        {
            var sut = new OrderServiceClient(BaseUrl, new AuthInfo(TestAccount.Email, TestAccountPassword));

            var orders = sut.GetOrder(new OrderRequest()
            {
                AccountId = TestAccount.Id,
                OrderId = _orderId
            });
            Assert.NotNull(orders);
            Assert.AreEqual("3939",orders.PickupApartment);
            Assert.AreEqual("1234 rue Saint-Hubert", orders.PickupAddress);
            Assert.AreEqual("3131", orders.PickupRingCode);
            Assert.AreEqual(45.515065, orders.PickupLatitude);
            Assert.AreEqual(-73.558064, orders.PickupLongitude);
            Assert.AreEqual("Velvet auberge st gabriel", orders.DropOffAddress);
            Assert.AreEqual(45.50643, orders.DropOffLatitude);
            Assert.AreEqual(-73.554052, orders.DropOffLongitude);
        }
    }
}