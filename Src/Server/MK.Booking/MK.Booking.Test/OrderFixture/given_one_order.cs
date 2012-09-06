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

        [Test]
        public void when_complete_order_successfully()
        {
            var completeOrder = new CompleteOrder {OrderId = _orderId, Date = DateTime.Now, Fare = 23, Toll = 2, Tip = 5};
            this.sut.When(completeOrder);

            var @event = sut.ThenHasSingle<OrderCompleted>();
            Assert.AreEqual(_orderId, @event.SourceId);
            Assert.That(@event.Date, Is.EqualTo(completeOrder.Date).Within(1).Seconds);
            Assert.AreEqual(completeOrder.Fare, @event.Fare);
            Assert.AreEqual(completeOrder.Toll, @event.Toll);
            Assert.AreEqual(completeOrder.Tip, @event.Tip);
        }

        [Test]
        public void when_complete_twice_order_one_event_only()
        {
            this.sut.Given(new OrderCompleted());
            this.sut.When(new CompleteOrder { OrderId = _orderId });

            sut.ThenHasSingle<OrderCompleted>();
        }

        [Test]
        public void when_remove_from_history_successfully()
        {
            this.sut.When(new RemoveOrderFromHistory() { OrderId = _orderId });

            sut.ThenHasSingle<OrderRemovedFromHistory>();
        }
    }
}
