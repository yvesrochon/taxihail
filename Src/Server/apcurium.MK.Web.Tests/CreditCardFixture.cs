﻿using System;
using System.Linq;
using NUnit.Framework;
using apcurium.MK.Booking.Api.Client.Cmt.Payments;
using apcurium.MK.Booking.Api.Client.Cmt.Payments.Authorization;
using apcurium.MK.Booking.Api.Client.TaxiHail;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Common;

namespace apcurium.MK.Web.Tests
{
    [TestFixture]
    public class CreditCardFixture : BaseTest
    {
        [TestFixtureSetUp]
        public override void TestFixtureSetup()
        {
            base.TestFixtureSetup();
        }

        [TestFixtureTearDown]
        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();
        }

        [Test]
        public void AddCreditCard()
        {

            const string creditCardComapny = "visa";
            const string friendlyName = "work credit card";
            var creditCardId = Guid.NewGuid();
            const string last4Digits = "4025";
            const string token = "jjwcnSLWm85";

            AccountService.AddCreditCard(new CreditCardRequest
            {
                CreditCardCompany = creditCardComapny,
                FriendlyName = friendlyName,
                CreditCardId = creditCardId,
                Last4Digits = last4Digits,
                Token = token
            });

            var creditCards = AccountService.GetCreditCards();
            var creditcard = creditCards.First(x => x.CreditCardId == creditCardId);
            Assert.NotNull(creditcard);
            Assert.AreEqual(TestAccount.Id, creditcard.AccountId);
            Assert.AreEqual(creditCardComapny, creditcard.CreditCardCompany);
            Assert.AreEqual(friendlyName, creditcard.FriendlyName);
            Assert.AreEqual(creditCardId, creditcard.CreditCardId);
            Assert.AreEqual(last4Digits, creditcard.Last4Digits);
            Assert.AreEqual(token, creditcard.Token);
        }


        [Test]
        [Ignore] //service CMT ne marche pas pour le moment
        public void RemoveCreditCard()
        {
            
            var client = GetCmtPaymentClient();

            var sut = new AccountServiceClient(BaseUrl, SessionId, client);

            const string creditCardComapny = "visa";
            const string friendlyName = "work credit card";
            var creditCardId = Guid.NewGuid();
            const string last4Digits = "4025";
            //const string token = "jjwcnSLWm85";

            DummyConfigManager.AddOrSet(AuthorizationRequest.CurrencyCodes.CurrencyCodeString, AuthorizationRequest.CurrencyCodes.Main.CanadaDollar);


            var tokenResponse = client.Tokenize(TestCreditCards.Discover.Number, TestCreditCards.Discover.ExpirationDate, "NOT USED");

            sut.AddCreditCard(new CreditCardRequest
            {
                CreditCardCompany = creditCardComapny,
                FriendlyName = friendlyName,
                CreditCardId = creditCardId,
                Last4Digits = last4Digits,
                Token = tokenResponse.CardOnFileToken
            });

            sut.RemoveCreditCard(creditCardId, tokenResponse.CardOnFileToken);

            var creditCards = sut.GetCreditCards();
            Assert.IsEmpty(creditCards.Where(x => x.CreditCardId == creditCardId));
        }

    }
}