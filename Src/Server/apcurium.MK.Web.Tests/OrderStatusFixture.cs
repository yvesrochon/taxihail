﻿using System;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using apcurium.MK.Booking.Api.Client;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Api.Contract.Resources;

namespace apcurium.MK.Web.Tests
{
    [TestFixture]
    public class OrderStatusFixture : BaseTest
    {

        private Guid _orderId;
       
        [TestFixtureSetUp]
        public override void TestFixtureSetup()
        {
            base.TestFixtureSetup();

            new AuthServiceClient(BaseUrl).Authenticate(TestAccount.Email, TestAccountPassword);   
 
            _orderId = Guid.NewGuid();
            var sut = new OrderServiceClient(BaseUrl);
            var order = new CreateOrder
            {
                Id = _orderId,
                PickupAddress = TestAddresses.GetAddress1(),
                DropOffAddress = TestAddresses.GetAddress2(),
                PickupDate = DateTime.Now,
                Settings = new BookingSettings { ChargeTypeId = 99, VehicleTypeId = 88, ProviderId = 11, Phone = "514-555-1212", Passengers = 6, NumberOfTaxi = 1, Name = "Joe Smith" }
            };

            sut.CreateOrder(order);
        }

        [TestFixtureTearDown]
        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();
        }

        [Test]
        public void create_and_get_a_valid_order()
        {
            var sut = new OrderServiceClient(BaseUrl);
            var data = sut.GetOrderStatus( _orderId);
            Assert.AreEqual("wosWAITING", data.IBSStatusId);
        }

        [Test]
        public void can_not_access_order_status_another_account()
        {
            CreateAndAuthenticateTestAccount();

            var sut = new OrderServiceClient(BaseUrl);

            Assert.Throws<WebServiceException>(() => sut.GetOrderStatus(_orderId));
        }
    }
}