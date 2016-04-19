﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
//using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.Security.Twitter;
using Microsoft.Practices.Unity;
using Owin;

namespace apcurium.MK.Web
{
    public partial class MkWebAppHost
    {
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Configure the application for OAuth based flow
            PublicClientId = "self";

            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions);
        }
    }
}
