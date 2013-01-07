﻿using System;
using System.Collections;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using apcurium.MK.Booking.Api.Contract.Http;
using apcurium.MK.Common.Entity;

namespace apcurium.MK.Booking.Api.Contract.Requests
{
    [Authenticate]
    [RestService("/account/orders/{OrderId}/status/", "GET")]
    public class OrderStatusRequest : BaseDTO
    {
        public Guid OrderId { get; set; }
    }

    [Authenticate]
    [RestService("/account/orders/status/", "GET")]
    public class MultipleOrderStatusRequest : BaseDTO
    {
        public Guid[] OrderIds { get; set; }
    }

    [NoCache]
    public class OrderStatusRequestResponse: OrderStatusDetail, IHasResponseStatus
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    [NoCache]
    public class MultipleOrderStatusRequestResponse: IEnumerable<OrderStatusDetail>, IHasResponseStatus
    {
        private readonly IList<OrderStatusDetail> _details;
        public MultipleOrderStatusRequestResponse()
        {
            _details = new List<OrderStatusDetail>();
        }

        public void Add(OrderStatusDetail detail)
        {
            _details.Add(detail);
        }

        public ResponseStatus ResponseStatus { get; set; }

        public IEnumerator<OrderStatusDetail> GetEnumerator()
        {
            return _details.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}