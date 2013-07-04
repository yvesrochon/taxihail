﻿using System;
using apcurium.MK.Booking.Api.Client.TaxiHail;
using apcurium.MK.Booking.Api.Contract.Requests.Braintree;
using apcurium.MK.Booking.Api.Contract.Resources.Payments;
using BraintreeEncryption.Library;

namespace apcurium.MK.Booking.Api.Client.Payments.Braintree
{
    public class BraintreeServiceClient : BaseServiceClient, IPaymentServiceClient
    {
        
        public BraintreeServiceClient(string url, string sessionId, string clientKey):base(url, sessionId)
        {
            //todo client side get
			ClientKey =clientKey;

        }

        protected string ClientKey { get; set; }


        public TokenizedCreditCardResponse Tokenize(string creditCardNumber, DateTime expiryDate, string cvv)
        {
            var braintree = new BraintreeEncrypter(ClientKey);
            var encryptedNumber = braintree.Encrypt(creditCardNumber);
            var encryptedExpirationDate = braintree.Encrypt(expiryDate.ToString("MM/yyyy"));
            var encryptedCvv = braintree.Encrypt(cvv);

            return Client.Post(new TokenizeCreditCardBraintreeRequest()
            {
                EncryptedCreditCardNumber = encryptedNumber,
                EncryptedExpirationDate = encryptedExpirationDate,
                EncryptedCvv = encryptedCvv,
            });
        }

        public DeleteTokenizedCreditcardResponse ForgetTokenizedCard(string cardToken)
        {
            return Client.Delete(new DeleteTokenizedCreditcardBraintreeRequest()
                {
                    CardToken = cardToken,
                });
        }

        public PreAuthorizePaymentResponse PreAuthorize(string cardToken, double amount, string orderNumber)
        {
            return Client.Post(new PreAuthorizePaymentBraintreeRequest()
                {
                    Amount = (decimal) amount,
                    CardToken = cardToken,
                    OrderNumber = orderNumber,
                });

        }

        public CommitPreauthorizedPaymentResponse CommitPreAuthorized(string transactionId, string orderNumber)
        {
            return Client.Post(new CommitPreauthorizedPaymentBraintreeRequest()
                {
                    TransactionId = transactionId,
                });
        }
    }
}
