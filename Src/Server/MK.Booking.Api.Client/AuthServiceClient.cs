﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.ServiceClient.Web;
#if !CLIENT
using ServiceStack.ServiceInterface.Auth;
#else
using ServiceStack.Common.ServiceClient.Web;


#endif

namespace apcurium.MK.Booking.Api.Client
{
    public class AuthServiceClient : BaseServiceClient
    {
        public AuthServiceClient(string url)
            : base(url)
        {
        }

        public AuthResponse Authenticate(string email, string password)
        {
            return Authenticate(new Auth
            {
                UserName = email,
                Password = password,
                RememberMe = true,
            }, "credentials");
        }

        public AuthResponse AuthenticateFacebook(string facebookId)
        {
            return Authenticate(new Auth
            {
                UserName = facebookId,
                RememberMe = true,
                Password = facebookId,
            }, "credentialsfb");
        }

        private AuthResponse Authenticate(Auth auth, string provider)
        {
            var cookieContainer = new CookieContainer();
            ServiceClientBase.HttpWebRequestFilter = req =>
            {
                req.CookieContainer = cookieContainer;
            };

            return Client.Post<AuthResponse>("/auth/" + provider , auth);
        }
    }
}
