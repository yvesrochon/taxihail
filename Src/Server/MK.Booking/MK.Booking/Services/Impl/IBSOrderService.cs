﻿using System;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Extensions;

namespace apcurium.MK.Booking.Services.Impl
{
    public class IbsOrderService : IIbsOrderService
    {
        private readonly IIBSServiceProvider _ibsServiceProvider;
        private readonly ILogger _logger;
        private readonly Resources.Resources _resources;

        public IbsOrderService(IIBSServiceProvider ibsServiceProvider, IServerSettings serverSettings, ILogger logger)
        {
            _ibsServiceProvider = ibsServiceProvider;
            _logger = logger;

            _resources = new Resources.Resources(serverSettings);
        }

        public void ConfirmExternalPayment(Guid orderId, int ibsOrderId, decimal totalAmount, decimal tipAmount, decimal meterAmount, string type, string provider, string transactionId,
                                           string authorizationCode, string cardToken, int accountId, string name, string phone, string email, string os, string userAgent, string companyKey,
                                           decimal fareAmount = 0, decimal extrasAmount = 0, decimal vatAmount = 0, decimal discountAmount = 0, decimal tollAmount = 0, decimal surchargeAmount = 0)
        {
            if (companyKey.HasValue())
            {
                _logger.LogMessage(string.Format("Confirming external payment on external company '{0}'", companyKey));
            }

            if (!_ibsServiceProvider.Booking(companyKey).ConfirmExternalPayment(orderId, ibsOrderId, totalAmount, tipAmount, meterAmount, type, provider, transactionId,
                            authorizationCode, cardToken, accountId, name, phone, email, os, userAgent, fareAmount, extrasAmount, vatAmount, discountAmount, tollAmount, surchargeAmount))
            {
                throw new Exception("Cannot send payment information to dispatch.");
            }
        }

        public void SendPaymentNotification(double totalAmount, double meterAmount, double tipAmount, string authorizationCode, string vehicleNumber, string companyKey)
        {
            if (companyKey.HasValue())
            {
                _logger.LogMessage(string.Format("Sending payment notification on external company '{0}'", companyKey));
            }

            var amountString = _resources.FormatPrice(totalAmount);
            var meterString = _resources.FormatPrice(meterAmount);
            var tipString = _resources.FormatPrice(tipAmount);

            // Padded with 32 char because the MDT displays line of 32 char.  This will cause to write each string on a new line
            var line1 = string.Format(_resources.Get("PaymentConfirmationToDriver1"));
            line1 = line1.PadRight(32, ' ');
            var line2 = string.Format(_resources.Get("PaymentConfirmationToDriver2"), meterString, tipString);
            line2 = line2.PadRight(32, ' ');
            var line3 = string.Format(_resources.Get("PaymentConfirmationToDriver3"), amountString);
            line3 = line3.PadRight(32, ' ');

            var line4 = string.IsNullOrWhiteSpace(authorizationCode)
                ? string.Empty
                : string.Format(_resources.Get("PaymentConfirmationToDriver4"), authorizationCode);

            if (!_ibsServiceProvider.Booking(companyKey).SendMessageToDriver(line1 + line2 + line3 + line4, vehicleNumber))
            {
                throw new Exception("Cannot send the payment notification.");
            }
        }

        public void SendMessageToDriver(string message, string vehicleNumber, string companyKey)
        {
            if (companyKey.HasValue())
            {
                _logger.LogMessage(string.Format("Sending message to driver on external company '{0}'", companyKey));
            }

            if (!_ibsServiceProvider.Booking(companyKey).SendMessageToDriver(message, vehicleNumber))
            {
                throw new Exception("Cannot send message to driver.");
            }
        }
    }
}