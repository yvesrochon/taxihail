﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Infrastructure.Messaging;
using ServiceStack.Common.Web;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceInterface;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.ReadModel.Query;

namespace apcurium.MK.Booking.Api.Services
{
    public class PopularAddressService: RestServiceBase<PopularAddress> 
    {
        public IValidator<PopularAddressService> Validator { get; set; }

        private readonly ICommandBus _commandBus;
        protected IPopularAddressDao Dao { get; set; }
        public PopularAddressService(IPopularAddressDao dao,ICommandBus commandBus)
        {
            _commandBus = commandBus;
            Dao = dao;
        }

        public override object OnGet(PopularAddress request)
        {
            return Dao.GetAll();
        }

        public override object OnPost(PopularAddress request)
        {
            var command = new Commands.AddPopularAddress();
            
            AutoMapper.Mapper.Map(request, command);

            _commandBus.Send(command);

            return new HttpResult(HttpStatusCode.OK);
        }

        public override object OnDelete(PopularAddress request)
        {
            var command = new Commands.RemovePopularAddress
            {
                Id = Guid.NewGuid(),
                AddressId = request.Id
            };

            _commandBus.Send(command);

            return new HttpResult(HttpStatusCode.OK);
        }

        public override object OnPut(PopularAddress request)
        {
            var command = new Commands.UpdatePopularAddress();

            AutoMapper.Mapper.Map(request, command);

            _commandBus.Send(command);

            return new HttpResult(HttpStatusCode.OK);
        }

    }
}