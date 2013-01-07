﻿using System.Net;
using System.Linq;
using Infrastructure.Messaging;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.Google.Resources;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Booking.ReadModel.Query;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Extensions;
using System.Globalization;
using System.Collections.Generic;
using System;
using ServiceStack.ServiceClient.Web;
using apcurium.MK.Common;
using apcurium.MK.Common.Diagnostic;
using log4net;
using ServiceStack.Text;
using OrderStatusDetail = apcurium.MK.Common.Entity.OrderStatusDetail;

namespace apcurium.MK.Booking.Api.Services
{
    public class OrderStatusService : RestServiceBase<Contract.Requests.OrderStatusRequest>
    {
        private readonly IOrderDao _orderDao;
        private readonly IAccountDao _accountDao;

        public OrderStatusService(IOrderDao orderDao, IAccountDao accountDao)
        {
            _accountDao = accountDao;
            _orderDao = orderDao;
        }

        public override object OnGet(OrderStatusRequest request)
        {
            var account = _accountDao.FindById(new Guid(this.GetSession().UserAuthId));

            return new OrderStatusHelper(_orderDao, account.Id)
                .GetOrderStatus<OrderStatusRequestResponse>(request.OrderId);
        }

    }

    public class MultipleOrderStatusService : RestServiceBase<Contract.Requests.MultipleOrderStatusRequest>
    {
        private readonly IOrderDao _orderDao;
        private readonly IAccountDao _accountDao;

        public MultipleOrderStatusService(IOrderDao orderDao, IAccountDao accountDao)
        {
            _accountDao = accountDao;
            _orderDao = orderDao;
        }

        public override object OnGet(Contract.Requests.MultipleOrderStatusRequest request)
        {
            var statuses = new MultipleOrderStatusRequestResponse();
            var account = _accountDao.FindById(new Guid(this.GetSession().UserAuthId));
            var helper = new OrderStatusHelper(_orderDao, account.Id);

            foreach (var id in request.OrderIds)
            {
                statuses.Add(helper.GetOrderStatus<OrderStatusDetail>(id));
            }

            return statuses;
        }
    }

    class OrderStatusHelper
    {
        private readonly IOrderDao _orderDao;
        private readonly Guid _accountId;

        public OrderStatusHelper(IOrderDao orderDao, Guid accountId)
        {
            _orderDao = orderDao;
            _accountId = accountId;
        }

        internal T GetOrderStatus<T>(Guid id) where T:OrderStatusDetail, new()
        {
            var order = _orderDao.FindById(id);

            if (order == null)
            {
                //Order could be null if creating the order takes a lot of time and this method is called before the create finishes
                return new T
                {
                    OrderId = id,
                    Status = apcurium.MK.Common.Entity.OrderStatus.Created,
                    IBSOrderId = 0,
                    IBSStatusId = "",
                    IBSStatusDescription = "Processing your order"
                };
            }

            if (_accountId != order.AccountId)
            {
                throw new HttpError(HttpStatusCode.Unauthorized, "Can't access another account's order");
            }

            if (!order.IBSOrderId.HasValue)
            {
                return new T { IBSStatusDescription = "Can't contact dispatch company" };
            }

            //TODO: Read OrderStatus table
            return new T
            {
                OrderId = id
            };
        }
    }
}