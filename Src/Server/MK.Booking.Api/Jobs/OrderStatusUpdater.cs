﻿#region

using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using apcurium.MK.Booking.Api.Contract.Requests.Payment;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Api.Helpers;
using apcurium.MK.Booking.Api.Services;
using apcurium.MK.Booking.Api.Services.Payment;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.Database;
using apcurium.MK.Booking.EventHandlers.Integration;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Booking.Maps;
using apcurium.MK.Booking.Maps.Geo;
using apcurium.MK.Booking.PushNotifications;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Booking.ReadModel.Query.Contract;
using apcurium.MK.Booking.Resources;
using apcurium.MK.Booking.Services;
using apcurium.MK.Common;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Configuration.Impl;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Enumeration;
using apcurium.MK.Common.Extensions;
using apcurium.MK.Common.Resources;
using Infrastructure.Messaging;
using System;
using log4net;
using ServiceStack.Common.Web;

#endregion

namespace apcurium.MK.Booking.Api.Jobs
{
    public class OrderStatusUpdater
    {
        private readonly ICommandBus _commandBus;
        private readonly IServerSettings _serverSettings;
        private readonly IOrderPaymentDao _orderPaymentDao;
        private readonly IOrderDao _orderDao;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly IDirections _directions;
        private readonly IIbsOrderService _ibsOrderService;
        private readonly IAccountDao _accountDao;
        private readonly ICreditCardDao _creditCardDao;
        private readonly Resources.Resources _resources;

        private static readonly ILog Log = LogManager.GetLogger(typeof(OrderStatusUpdater));

        private string _languageCode = "";

        public OrderStatusUpdater(IServerSettings serverSettings, 
            ICommandBus commandBus, 
            IOrderPaymentDao orderPaymentDao, 
            IOrderDao orderDao,
            IPaymentService paymentService,
            INotificationService notificationService,
            IDirections directions,
            IIbsOrderService ibsOrderService,
            IAccountDao accountDao,
            ICreditCardDao creditCardDao)
        {
            _orderDao = orderDao;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _directions = directions;
            _ibsOrderService = ibsOrderService;
            _serverSettings = serverSettings;
            _accountDao = accountDao;
            _creditCardDao = creditCardDao;
            _commandBus = commandBus;
            _orderPaymentDao = orderPaymentDao;
            _resources = new Resources.Resources(serverSettings);
        }

        public void Update(IBSOrderInformation orderFromIbs, OrderStatusDetail orderStatusDetail)
        {
            UpdateVehiclePositionAndSendNearbyNotificationIfNecessary(orderFromIbs, orderStatusDetail);

            if (orderFromIbs.IsWaitingToBeAssigned)
            {
                CheckForOrderTimeOut(orderStatusDetail);
            }

            if (!OrderNeedsUpdate(orderFromIbs, orderStatusDetail))
            {
                return;
            }

            PopulateFromIbsOrder(orderStatusDetail, orderFromIbs);

            CheckForPairingAndHandleIfNecessary(orderStatusDetail, orderFromIbs);

            _commandBus.Send(new ChangeOrderStatus
            {
                Status = orderStatusDetail,
                Fare = orderFromIbs.Fare,
                Toll = orderFromIbs.Toll,
                Tip = orderFromIbs.Tip,
                Tax = orderFromIbs.VAT,
            });
        }

        private void PopulateFromIbsOrder(OrderStatusDetail orderStatusDetail, IBSOrderInformation ibsOrderInfo)
        {
            orderStatusDetail.IBSStatusId =                     ibsOrderInfo.Status;
            orderStatusDetail.DriverInfos.FirstName =           ibsOrderInfo.FirstName.GetValue(orderStatusDetail.DriverInfos.FirstName);
            orderStatusDetail.DriverInfos.LastName =            ibsOrderInfo.LastName.GetValue(orderStatusDetail.DriverInfos.LastName);
            orderStatusDetail.DriverInfos.MobilePhone =         ibsOrderInfo.MobilePhone.GetValue(orderStatusDetail.DriverInfos.MobilePhone);
            orderStatusDetail.DriverInfos.VehicleColor =        ibsOrderInfo.VehicleColor.GetValue(orderStatusDetail.DriverInfos.VehicleColor);
            orderStatusDetail.DriverInfos.VehicleMake =         ibsOrderInfo.VehicleMake.GetValue(orderStatusDetail.DriverInfos.VehicleMake);
            orderStatusDetail.DriverInfos.VehicleModel =        ibsOrderInfo.VehicleModel.GetValue(orderStatusDetail.DriverInfos.VehicleModel);
            orderStatusDetail.DriverInfos.VehicleRegistration = ibsOrderInfo.VehicleRegistration.GetValue(orderStatusDetail.DriverInfos.VehicleRegistration);
            orderStatusDetail.DriverInfos.VehicleType =         ibsOrderInfo.VehicleType.GetValue(orderStatusDetail.DriverInfos.VehicleType);
            orderStatusDetail.DriverInfos.DriverId =            ibsOrderInfo.DriverId.GetValue(orderStatusDetail.DriverInfos.DriverId);
            orderStatusDetail.VehicleNumber =                   ibsOrderInfo.VehicleNumber.GetValue(orderStatusDetail.VehicleNumber);
            orderStatusDetail.TerminalId =                      ibsOrderInfo.TerminalId.GetValue(orderStatusDetail.TerminalId);
            orderStatusDetail.ReferenceNumber =                 ibsOrderInfo.ReferenceNumber.GetValue(orderStatusDetail.ReferenceNumber);
            orderStatusDetail.Eta =                             ibsOrderInfo.Eta ?? orderStatusDetail.Eta;
            
            UpdateStatusIfNecessary(orderStatusDetail, ibsOrderInfo);

            orderStatusDetail.IBSStatusDescription = GetDescription(orderStatusDetail.OrderId, ibsOrderInfo, orderStatusDetail.CompanyName);
        }

        private void UpdateStatusIfNecessary(OrderStatusDetail orderStatusDetail, IBSOrderInformation ibsOrderInfo)
        {
            if (orderStatusDetail.Status == OrderStatus.WaitingForPayment
                || (orderStatusDetail.Status == OrderStatus.TimedOut && ibsOrderInfo.IsWaitingToBeAssigned))
            {
                Log.DebugFormat("Order {1}: Status is: {0}. Don't update since it's a special case outside of IBS.", orderStatusDetail.Status, orderStatusDetail.OrderId);
                return;
            }

            if (orderStatusDetail.Status == OrderStatus.TimedOut && !ibsOrderInfo.IsWaitingToBeAssigned)
            {
                // Ride was assigned while waiting for user input on whether or not to switch company
                orderStatusDetail.Status = OrderStatus.Created;
            }
            
            if (ibsOrderInfo.IsCanceled)
            {
                orderStatusDetail.Status = OrderStatus.Canceled;
                ChargeNoShowFeeIfNecessary(ibsOrderInfo, orderStatusDetail);
                Log.DebugFormat("Order {1}: Status updated to: {0}", orderStatusDetail.Status, orderStatusDetail.OrderId);
            }
            else if (ibsOrderInfo.IsTimedOut)
            {
                orderStatusDetail.Status = OrderStatus.TimedOut;
                Log.DebugFormat("Order {1}: Status updated to: {0}", orderStatusDetail.Status, orderStatusDetail.OrderId);
            }
            else if (ibsOrderInfo.IsComplete)
            {
                orderStatusDetail.Status = OrderStatus.Completed;
                Log.DebugFormat("Order {1}: Status updated to: {0}", orderStatusDetail.Status, orderStatusDetail.OrderId);
            }
        }

        private PreAuthorizePaymentResponse PreauthorizePaymentIfNecessary(Guid orderId, string cardToken, decimal amount)
        {
            if (_serverSettings.GetPaymentSettings().IsPreAuthEnabled)
            {
                // Already preautorized on create order, do nothing
                return new PreAuthorizePaymentResponse { IsSuccessful = true };
            }

            var orderDetail = _orderDao.FindById(orderId);
            if (orderDetail == null)
            {
                throw new HttpError(HttpStatusCode.NotFound, "Order not found");
            }

            var account = _accountDao.FindById(orderDetail.AccountId);

            return _paymentService.PreAuthorize(orderId, account.Email, cardToken, amount);
        }

        private void ChargeNoShowFeeIfNecessary(IBSOrderInformation ibsOrderInfo, OrderStatusDetail orderStatusDetail)
        {
            if (ibsOrderInfo.Status != VehicleStatuses.Common.NoShow)
            {
                return;
            }

            Log.DebugFormat("No show fee will be charged for order {0}.", ibsOrderInfo.IBSOrderId);

            var paymentSettings = _serverSettings.GetPaymentSettings();
            var account = _accountDao.FindById(orderStatusDetail.AccountId);

            if (paymentSettings.NoShowFee.HasValue
                && paymentSettings.NoShowFee.Value > 0
                && account.Settings.ChargeTypeId == ChargeTypes.CardOnFile.Id)
            {
                var defaultCreditCard = _creditCardDao.FindByAccountId(account.Id).FirstOrDefault();

                if (defaultCreditCard != null)
                {
                    try
                    {
                        // PreAuthorization
                        var preAuthResponse = PreauthorizePaymentIfNecessary(orderStatusDetail.OrderId, defaultCreditCard.Token, paymentSettings.NoShowFee.Value);
                        if (preAuthResponse.IsSuccessful)
                        {
                            // Wait for OrderPaymentDetail to be created
                            Thread.Sleep(500);

                            // Commit
                            var paymentResult = _paymentService.CommitPayment(
                                paymentSettings.NoShowFee.Value, paymentSettings.NoShowFee.Value,
                                0, defaultCreditCard.Token, orderStatusDetail.OrderId, true);

                            if (paymentResult.IsSuccessful)
                            {
                                Log.DebugFormat("No show fee of amount {0} was charged for order {1}.", paymentSettings.NoShowFee.Value, ibsOrderInfo.IBSOrderId);
                            }
                            else
                            {
                                orderStatusDetail.PairingError = paymentResult.Message;
                                Log.DebugFormat("Could not process no show fee for order {0}: {1}.", ibsOrderInfo.IBSOrderId, paymentResult.Message);
                            }
                        }
                        else
                        {
                            orderStatusDetail.PairingError = preAuthResponse.Message;
                            Log.DebugFormat("Could not process no show fee for order {0}: {1}.", ibsOrderInfo.IBSOrderId, preAuthResponse.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        orderStatusDetail.PairingError = ex.Message;
                        Log.DebugFormat("Could not process no show fee for order {0}: {1}.", ibsOrderInfo.IBSOrderId, ex.Message);
                    }
                }
            }
        }

        private void UpdateVehiclePositionAndSendNearbyNotificationIfNecessary(IBSOrderInformation ibsOrderInfo, OrderStatusDetail orderStatus)
        {
            if (orderStatus.VehicleLatitude != ibsOrderInfo.VehicleLatitude
                || orderStatus.VehicleLongitude != ibsOrderInfo.VehicleLongitude)
            {
                _orderDao.UpdateVehiclePosition(orderStatus.OrderId, ibsOrderInfo.VehicleLatitude, ibsOrderInfo.VehicleLongitude);
                _notificationService.SendTaxiNearbyPush(orderStatus.OrderId, ibsOrderInfo.Status, ibsOrderInfo.VehicleLatitude, ibsOrderInfo.VehicleLongitude);

                Log.DebugFormat("Vehicle position updated. New position: ({0}, {1}).", ibsOrderInfo.VehicleLatitude, ibsOrderInfo.VehicleLongitude);
            }
        }

        private void HandlePairingForRideLinqCmt(OrderPairingDetail pairingInfo, IBSOrderInformation ibsOrderInfo)
        {
            // in the case of RideLinq CMT, we only want to calculate the tip to fill information on our side
            if (pairingInfo.AutoTipPercentage.HasValue)
            {
                ibsOrderInfo.Tip = GetTipAmount(ibsOrderInfo.Fare, pairingInfo.AutoTipPercentage.Value);
                Log.DebugFormat("RideLinqCmt Pairing: Calculated a tip amount of {0}, based on an auto AutoTipPercentage percentage of {1}",
                    ibsOrderInfo.Tip, pairingInfo.AutoTipPercentage.Value);
            }
            else
            {
                Log.Debug("RideLinqCmt Pairing: AutoTipPercentage is null, no tip amount was assigned.");
            }
        }

        private void HandlePairingForStandardPairing(OrderStatusDetail orderStatusDetail, OrderPairingDetail pairingInfo, IBSOrderInformation ibsOrderInfo)
        {
            if (!_serverSettings.GetPaymentSettings().AutomaticPayment)
            {
                Log.Debug("Standard Pairing: Automatic payment is disabled, nothing else to do.");
                return;
            }

            var orderPayment = _orderPaymentDao.FindNonPayPalByOrderId(orderStatusDetail.OrderId);
            if (orderPayment != null && (orderPayment.IsCompleted || orderPayment.IsCancelled))
            {
                // Payment was already processed
                Log.DebugFormat("Payment for order {0} was already processed, nothing else to do.", orderStatusDetail.OrderId);
                return;
            }

            if (ibsOrderInfo.IsMeterOffNotPaid)
            {
                SendPaymentBeingProcessedMessageToDriver(ibsOrderInfo.VehicleNumber);
            }

            if (ibsOrderInfo.Fare <= 0)
            {
                // fare was not returned by ibs
                // check if status is completed
                if (orderStatusDetail.Status == OrderStatus.Completed)
                {
                    // no fare received but order is completed, change status to increase polling speed
                    orderStatusDetail.Status = OrderStatus.WaitingForPayment;
                    orderStatusDetail.PairingTimeOut = DateTime.UtcNow.AddMinutes(30);
                    Log.DebugFormat("Order {1}: Status updated to: {0} with timeout in 30 minutes", orderStatusDetail.Status, orderStatusDetail.OrderId);
                }

                if (orderStatusDetail.Status == OrderStatus.WaitingForPayment
                    && DateTime.UtcNow > orderStatusDetail.PairingTimeOut)
                {
                    orderStatusDetail.Status = OrderStatus.Completed;
                    orderStatusDetail.PairingError = "Timed out period reached while waiting for payment informations from IBS.";
                    Log.ErrorFormat("Order {1}: Pairing error: {0}", orderStatusDetail.PairingError, orderStatusDetail.OrderId);
                }

                return;
            }

            // We received a fare from IBS
            // Send payment for capture, once it's captured, we will set the status to Completed
            var meterAmount = ibsOrderInfo.Fare + ibsOrderInfo.Toll + ibsOrderInfo.VAT;
            double tipPercentage = pairingInfo.AutoTipPercentage ?? _serverSettings.ServerData.DefaultTipPercentage;
            var tipAmount = GetTipAmount(meterAmount, tipPercentage);

            Log.DebugFormat(
                    "Order {4}: Received total amount from IBS of {0}, calculated a tip of {1}% (tip amount: {2}), for a total of {3}",
                    meterAmount, tipPercentage, tipAmount, meterAmount + tipAmount, orderStatusDetail.OrderId);

            if (!_serverSettings.ServerData.SendDetailedPaymentInfoToDriver)
            {
                // this is the only payment related message sent to the driver when this setting is false
                SendMinimalPaymentProcessedMessageToDriver(ibsOrderInfo.VehicleNumber, meterAmount + tipAmount, meterAmount, tipAmount);
            }

            try
            {
                var totalOrderAmout = Convert.ToDecimal(meterAmount + tipAmount);

                // Preautorize
                var preAuthResponse = PreauthorizePaymentIfNecessary(orderStatusDetail.OrderId, pairingInfo.TokenOfCardToBeUsedForPayment, totalOrderAmout);
                if (preAuthResponse.IsSuccessful)
                {
                    // Wait for OrderPaymentDetail to be created
                    Thread.Sleep(500);

                    // Commit
                    var paymentResult = _paymentService.CommitPayment(
                        totalOrderAmout, Convert.ToDecimal(meterAmount),
                        Convert.ToDecimal(tipAmount), pairingInfo.TokenOfCardToBeUsedForPayment,
                        orderStatusDetail.OrderId, false);

                    if (paymentResult.IsSuccessful)
                    {
                        Log.DebugFormat("Order {0}: Payment Successful (Auth: {1}) [Transaction Id: {2}]", orderStatusDetail.OrderId, paymentResult.AuthorizationCode, paymentResult.TransactionId);
                    }
                    else
                    {
                        if (_serverSettings.ServerData.SendDetailedPaymentInfoToDriver)
                        {
                            _ibsOrderService.SendMessageToDriver(_resources.Get("PaymentFailedToDriver"), orderStatusDetail.VehicleNumber);
                        }

                        // set the payment error message in OrderStatusDetail for reporting purpose
                        orderStatusDetail.PairingError = paymentResult.Message;

                        Log.ErrorFormat("Order {0}: Payment FAILED (Message: {1}) [Transaction Id: {2}]", orderStatusDetail.OrderId, paymentResult.Message, paymentResult.TransactionId);
                    }
                }
                else
                {
                    orderStatusDetail.PairingError = preAuthResponse.Message;
                    Log.DebugFormat("Could not process no show fee for order {0}: {1}.", ibsOrderInfo.IBSOrderId, preAuthResponse.Message);
                }
            }
            catch (Exception ex)
            {
                if (_serverSettings.ServerData.SendDetailedPaymentInfoToDriver)
                {
                    _ibsOrderService.SendMessageToDriver(_resources.Get("PaymentFailedToDriver"), orderStatusDetail.VehicleNumber);
                }

                // set the payment error message in OrderStatusDetail for reporting purpose
                orderStatusDetail.PairingError = ex.Message;

                Log.ErrorFormat("Order {0}: Payment FAILED (Message: {1}) [Transaction Id: {2}]", orderStatusDetail.OrderId, ex.Message, "UNKNOWN");
            }
            
            // whether there's a success or not, we change the status back to Completed since we can't process the payment again
            orderStatusDetail.Status = OrderStatus.Completed;
        }
        
        private double GetTipAmount(double amount, double percentage)
        {
            var tip = percentage / 100;
            return Math.Round(amount * tip, 2);
        }

        private void CheckForPairingAndHandleIfNecessary(OrderStatusDetail orderStatusDetail, IBSOrderInformation ibsOrderInfo)
        {
            var pairingInfo = _orderDao.FindOrderPairingById(orderStatusDetail.OrderId);
            if (pairingInfo == null)
            {
                Log.DebugFormat("Order {0}: No pairing to process as no pairing information was found.", orderStatusDetail.OrderId);
                return;
            }

            var paymentMode = _serverSettings.GetPaymentSettings().PaymentMode;
            switch (paymentMode)
            {
                case PaymentMethod.Cmt:
                case PaymentMethod.Braintree:
                case PaymentMethod.Moneris:
                    HandlePairingForStandardPairing(orderStatusDetail, pairingInfo, ibsOrderInfo);
                    break;
                case PaymentMethod.RideLinqCmt:
                    HandlePairingForRideLinqCmt(pairingInfo, ibsOrderInfo);
                    break;
                case PaymentMethod.None:
                case PaymentMethod.Fake:
                    throw new NotImplementedException("Cannot have pairing without any payment mode");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool OrderNeedsUpdate(IBSOrderInformation ibsOrderInfo, OrderStatusDetail orderStatusDetail)
        {
            return (ibsOrderInfo.Status.HasValue() && orderStatusDetail.IBSStatusId != ibsOrderInfo.Status) // ibs status changed
                   || (!orderStatusDetail.FareAvailable && ibsOrderInfo.Fare > 0) // fare was not available and ibs now has the information
                   || orderStatusDetail.Status == OrderStatus.WaitingForPayment   // special case for pairing
                   || orderStatusDetail.Status == OrderStatus.TimedOut;           // special case for network
        }

        private void CheckForOrderTimeOut(OrderStatusDetail orderStatusDetail)
        {
            if (!_serverSettings.ServerData.Network.Enabled
                || orderStatusDetail.Status == OrderStatus.TimedOut
                || orderStatusDetail.IgnoreDispatchCompanySwitch)
            {
                // Nothing to do
                return;
            }

            if (orderStatusDetail.NetworkPairingTimeout.HasValue
                && orderStatusDetail.NetworkPairingTimeout.Value <= DateTime.UtcNow)
            {
                // Order timed out
                _commandBus.Send(new NotifyOrderTimedOut { OrderId = orderStatusDetail.OrderId });
            }
        }

        private string GetDescription(Guid orderId, IBSOrderInformation ibsOrderInfo, string companyName)
        {
            var orderDetail = _orderDao.FindById(orderId);
            _languageCode = orderDetail != null ? orderDetail.ClientLanguageCode : SupportedLanguages.en.ToString();

            string description = null;
            if (ibsOrderInfo.IsWaitingToBeAssigned)
            {
                if (companyName.HasValue())
                {
                    description = string.Format(_resources.Get("OrderStatus_wosWAITINGRoaming", _languageCode), companyName);
                    Log.DebugFormat("Setting Waiting in roaming status description: {0}", description);
                }
            }
            else if (ibsOrderInfo.IsAssigned)
            {
                description = string.Format(_resources.Get("OrderStatus_CabDriverNumberAssigned", _languageCode), ibsOrderInfo.VehicleNumber);
                Log.DebugFormat("Setting Assigned status description: {0}", description);

                if (_serverSettings.ServerData.ShowEta)
                {
                    try
                    {
                        SendEtaMessageToDriver((double) ibsOrderInfo.VehicleLatitude, (double) ibsOrderInfo.VehicleLongitude, 
                            orderDetail.PickupAddress.Latitude, orderDetail.PickupAddress.Longitude, ibsOrderInfo.VehicleNumber);
                    }
                    catch
                    {
                        Log.Error("Cannot Send Eta to Vehicle Number " + ibsOrderInfo.VehicleNumber);
                    }
                }
            }
            else if (ibsOrderInfo.IsCanceled)
            {
                description = _resources.Get("OrderStatus_" + ibsOrderInfo.Status, _languageCode);
                Log.DebugFormat("Setting Canceled status description: {0}", description);
            }
            else if (ibsOrderInfo.IsComplete)
            {
                description = _resources.Get("OrderStatus_wosDONE", _languageCode);    
                Log.DebugFormat("Setting Complete status description: {0}", description);
            }
            else if (ibsOrderInfo.IsLoaded)
            {
                if (orderDetail != null && (_serverSettings.GetPaymentSettings().AutomaticPayment
                                            && _serverSettings.GetPaymentSettings().AutomaticPaymentPairing
                                            && orderDetail.Settings.ChargeTypeId == ChargeTypes.CardOnFile.Id))
                {
                    description = _resources.Get("OrderStatus_wosLOADEDAutoPairing", _languageCode);
                }
            }

            return description.HasValue()
                        ? description
                        : _resources.Get("OrderStatus_" + ibsOrderInfo.Status, _languageCode);
        }

        private void SendEtaMessageToDriver(double vehicleLatitude, double vehicleLongitude, double pickupLatitude, double pickupLongitude, string vehicleNumber)
        {
            var eta = _directions.GetEta(vehicleLatitude, vehicleLongitude, pickupLatitude, pickupLongitude);
            if (eta != null && eta.IsValidEta())
            {
                var etaMessage = string.Format(_resources.Get("EtaMessageToDriver"), eta.FormattedDistance, eta.Duration);
                _ibsOrderService.SendMessageToDriver(etaMessage, vehicleNumber);
                Log.Debug(etaMessage);
            }
        }

        private void SendPaymentBeingProcessedMessageToDriver(string vehicleNumber)
        {
            var paymentBeingProcessedMessage = _resources.Get("PaymentBeingProcessedMessageToDriver");
            _ibsOrderService.SendMessageToDriver(paymentBeingProcessedMessage, vehicleNumber);
            Log.Debug(paymentBeingProcessedMessage);
        }

        private void SendMinimalPaymentProcessedMessageToDriver(string vehicleNumber, double amount, double meter, double tip)
        {
            _ibsOrderService.SendPaymentNotification(amount, meter, tip, null, vehicleNumber);
        }
    }
}