﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.ReadModel.Query.Contract;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Entity;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using ServiceStack.ServiceInterface;

namespace apcurium.MK.Booking.Api.Services
{
    public class ManualRidelinqOrderService : Service
    {
        private IOrderDao _orderDao;
        private ILogger _logger;
        private readonly ICommandBus _commandBus;

        public ManualRidelinqOrderService(ILogger logger, ICommandBus commandBus, IOrderDao orderDao)
        {
            _logger = logger;
            _commandBus = commandBus;
            _orderDao = orderDao;
        }

        public object Post(string ridelinqId)
        {
            var accountId = new Guid(this.GetSession().UserAuthId);

            var command = new CreateOrderForManualRideLinqPair
            {
                AccountId = accountId,
                UserAgent = Request.UserAgent,
                ClientVersion = Request.Headers.Get("ClientVersion")
            };

            _commandBus.Send(command);

            return new OrderStatusDetail
            {
                OrderId = command.Id,
                Status = OrderStatus.Created,
                IBSStatusId = string.Empty,
                IBSStatusDescription = string.Empty,
            };
        }

        public object Get(Guid orderId)
        {
            var item = _orderDao.GetManualRideLinqById(orderId);

            return new OrderMapper().ToResource(item);
        }

        public object Delete(Guid orderId)
        {
            _commandBus.Send(new UnpairOrderForManualRideLinq {OrderId = orderId});

            return "ok";
        }
    }
}
