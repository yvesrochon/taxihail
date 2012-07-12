﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using apcurium.MK.Booking.CommandHandlers;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.Common.Tests;
using apcurium.MK.Booking.Domain;
using apcurium.MK.Booking.Events;

namespace apcurium.MK.Booking.Test.OrderFixture
{
    [TestFixture]
    public class given_one_order
    {

        private EventSourcingTestHelper<Order> sut;
        private Guid _orderId = Guid.NewGuid();
        private Guid _accountId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            this.sut = new EventSourcingTestHelper<Order>();
            this.sut.Setup(new OrderCommandHandler(this.sut.Repository));
            this.sut.Given(new AccountRegistered { SourceId = _accountId, Name = "Bob", Password = null, Email = "bob.smith@apcurium.com" });
            this.sut.Given(new OrderCreated { SourceId = _orderId, AccountId = Guid.NewGuid(), PickupDate = DateTime.Now, PickupApartment = "3939", PickupAddress = "1234 rue Saint-Hubert", PickupRingCode = "3131", PickupLatitude = 45.515065, PickupLongitude = -73.558064 });
        }

        [Test]
        public void when_cancelling_successfully()
        {
            this.sut.When(new CancelOrder { OrderId = _orderId });
            
            var @event = sut.ThenHasSingle<OrderCancelled>();            
            Assert.AreEqual(_orderId, @event.SourceId);
        }
    }
}
