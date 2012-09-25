﻿using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;
using apcurium.MK.Booking.Database;
using apcurium.MK.Booking.EventHandlers;
using apcurium.MK.Booking.Events;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Common.Entity;

namespace apcurium.MK.Booking.Test.Integration.OrderFixture
{
    public class given_a_view_model_generator : given_a_read_model_database
    {
        protected OrderGenerator sut;
        protected List<ICommand> commands = new List<ICommand>();

        public given_a_view_model_generator()
        {
            var bus = new Mock<ICommandBus>();
            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(x => this.commands.Add(x.Body));
            bus.Setup(x => x.Send(It.IsAny<IEnumerable<Envelope<ICommand>>>()))
                .Callback<IEnumerable<Envelope<ICommand>>>(x => this.commands.AddRange(x.Select(e => e.Body)));

            this.sut = new OrderGenerator(() => new BookingDbContext(dbName));
        }
    }

    [TestFixture]
    public class given_no_order : given_a_view_model_generator
    {
        [Test]
        public void when_order_created_then_order_dto_populated()
        {
            var orderId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var pickupDate = DateTime.Now.AddDays(1);
            var createdDate = DateTime.Now;
            this.sut.Handle(new OrderCreated
            {
                SourceId = orderId,
                AccountId = accountId,
                PickupAddress = new Address
                                    {
                                        Apartment = "3939",
                                        Street = "1234 rue Saint-Hubert",
                                        RingCode = "3131",
                                        Latitude = 45.515065,
                                        Longitude = -73.558064
                                    },
              
                PickupDate = pickupDate,
                DropOffAddress =  new Address
                                    {
                                       FriendlyName = "Velvet auberge st gabriel",
                                       Latitude = 45.50643,
                                       Longitude = -73.554052,
                                    },
                CreatedDate = createdDate
            });

            using (var context = new BookingDbContext(dbName))
            {
                var list = context.Query<OrderDetail>().Where(x => x.Id == orderId);
                Assert.AreEqual(1, list.Count());
                var dto = list.Single();
                Assert.AreEqual(accountId, dto.AccountId);
                Assert.AreEqual("3939", dto.PickupAddress.Apartment);
                Assert.AreEqual("1234 rue Saint-Hubert", dto.PickupAddress.Street);
                Assert.AreEqual("3131", dto.PickupAddress.RingCode);
                Assert.AreEqual(45.515065, dto.PickupAddress.Latitude);
                Assert.AreEqual(-73.558064, dto.PickupAddress.Longitude);
                Assert.AreEqual("Velvet auberge st gabriel", dto.DropOffAddress.FriendlyName);
                Assert.AreEqual(45.50643, dto.DropOffAddress.Latitude);
                Assert.AreEqual(-73.554052, dto.DropOffAddress.Longitude);
                Assert.AreEqual(pickupDate.ToLongDateString(), dto.PickupDate.ToLongDateString());
            }
        }
    }

    [TestFixture]
    public class given_existing_order : given_a_view_model_generator
    {
        private Guid _orderId = Guid.NewGuid();
        private Guid _accountId = Guid.NewGuid();

        public given_existing_order()
        {
            var pickupDate = DateTime.Now;
            var requestDate = DateTime.Now.AddHours(1);

            this.sut.Handle(new OrderCreated()
                                {
                                    SourceId = _orderId,
                                    AccountId = _accountId,
                                    PickupAddress = new Address
                                    {
                                        Apartment = "3939",
                                        Street = "1234 rue Saint-Hubert",
                                        RingCode = "3131",
                                        Latitude = 45.515065,
                                        Longitude = -73.558064
                                    },
                                    PickupDate = pickupDate,
                                    DropOffAddress = new Address
                                    {
                                        Street = "Velvet auberge st gabriel",
                                        Latitude = 45.50643,
                                        Longitude = -73.554052,
                                    },
                                    CreatedDate = DateTime.Now,                                    
                                });

        }

        [Test]
        public void status_set_to_created()
        {
            using (var context = new BookingDbContext(dbName))
            {
                var dto = context.Find<OrderDetail>(_orderId);
                Assert.NotNull(dto);
                Assert.NotNull(dto.Status == (int)OrderStatus.Created );
            }
        }

        [Test]
        public void when_removed_then_no_longer_in_db()
        {
            var orderRemovedFromHistory = new OrderRemovedFromHistory(){ SourceId = _orderId };
            this.sut.Handle(orderRemovedFromHistory);

            using (var context = new BookingDbContext(dbName))
            {
                var dto = context.Find<OrderDetail>(_orderId);
                Assert.IsNull(dto);
            }
        }

        [Test]
        public void when_order_completed_then_order_dto_populated()
        {
            var orderCompleted = new OrderCompleted
                                     {
                                         SourceId = _orderId, Date = DateTime.Now, Fare = 23, Toll = 2, Tip = 5
                                     };
            this.sut.Handle(orderCompleted);

            using (var context = new BookingDbContext(dbName))
            {
                var dto = context.Find<OrderDetail>(_orderId);
                Assert.NotNull(dto);
                Assert.NotNull(dto.Status == (int)OrderStatus.Completed);
                Assert.AreEqual(orderCompleted.Fare, dto.Fare);
                Assert.AreEqual(orderCompleted.Toll, dto.Toll);
                Assert.AreEqual(orderCompleted.Tip, dto.Tip);
            }
        }
    }

}
