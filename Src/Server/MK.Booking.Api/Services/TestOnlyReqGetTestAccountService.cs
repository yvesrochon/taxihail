﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using ServiceStack.ServiceInterface;
using apcurium.MK.Booking.ReadModel.Query;
using apcurium.MK.Booking.Api.Contract.Requests;
using ServiceStack.ServiceInterface.Auth;
using apcurium.MK.Booking.Api.Contract.Requests;
using Infrastructure.Messaging;
using System.Threading;
using ServiceStack.ServiceHost;
using ServiceStack.Common.Web;


namespace apcurium.MK.Booking.Api.Services
{
    public class TestOnlyReqGetTestAccountService : RestServiceBase<TestOnlyReqGetTestAccount>
    {

        private ICommandBus _commandBus;
        private IAccountDao _dao;

        public TestOnlyReqGetTestAccountService(IAccountDao dao, ICommandBus commandBus)
        {
            _dao = dao;
            _commandBus = commandBus;
        }

        protected string TestUserEmail { get { return "apcurium.test{0}@apcurium.com"; } }

        protected string TestUserPassword { get { return "password1"; } }




        public override object OnGet(TestOnlyReqGetTestAccount request)
        {
            //This method can only be used for unit test.  
            //if (!((RequestContext.EndpointAttributes & EndpointAttributes.Localhost) == EndpointAttributes.Localhost))
            //{
            //    throw HttpError.NotFound("This method can only be called from the server");
            //}

            string testEmail = String.Format(TestUserEmail, request.Index);

            var testAccount = _dao.FindByEmail(testEmail);
            if (testAccount != null)
            {
                return testAccount;
            }


            var command = new Commands.RegisterAccount
            {
                AccountId = Guid.NewGuid(),
                Email = testEmail,
                Password = TestUserPassword,
                Name = "Test",
                Phone = "123456",
                IbsAccountId = 50860,
                ConfimationToken = Guid.NewGuid().ToString("N"),
                Language = "en",
                IsAdmin = false
            };

            _commandBus.Send(command);

            Thread.Sleep(400);
            // Confirm account immediately
            _commandBus.Send(new Commands.ConfirmAccount
            {
                 AccountId = command.AccountId,
                 ConfimationToken = command.ConfimationToken
            });

            return _dao.FindByEmail(testEmail);
        }

    }
}
