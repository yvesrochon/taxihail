﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.ReadModel.Query.Contract;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Configuration.Impl;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Enumeration;
using apcurium.MK.Common.Extensions;
using apcurium.MK.Common.Resources;
using CMTPayment;
using CMTPayment.Authorization;
using CMTPayment.Pair;
using CMTPayment.Reverse;
using CMTPayment.Tokenize;
using Infrastructure.Messaging;
using ServiceStack.Text;

namespace apcurium.MK.Booking.Services.Impl
{
    public class CmtPaymentService : IPaymentService
    {
        private readonly ICommandBus _commandBus;
        private readonly IOrderDao _orderDao;
        private readonly IAccountDao _accountDao;
        private readonly IServerSettings _serverSettings;
        private readonly IPairingService _pairingService;
        private readonly ILogger _logger;
        private readonly IOrderPaymentDao _paymentDao;
        private readonly CmtPaymentServiceClient _cmtPaymentServiceClient;
        private readonly CmtMobileServiceClient _cmtMobileServiceClient;

        public CmtPaymentService(ICommandBus commandBus, 
            IOrderDao orderDao,
            ILogger logger, 
            IAccountDao accountDao, 
            IOrderPaymentDao paymentDao,
            IServerSettings serverSettings,
            IPairingService pairingService)
        {
            _commandBus = commandBus;
            _orderDao = orderDao;
            _logger = logger;
            _accountDao = accountDao;
            _paymentDao = paymentDao;
            _serverSettings = serverSettings;
            _pairingService = pairingService;

            _cmtPaymentServiceClient = new CmtPaymentServiceClient(serverSettings.GetPaymentSettings().CmtPaymentSettings, null, null, logger);
            _cmtMobileServiceClient = new CmtMobileServiceClient(serverSettings.GetPaymentSettings().CmtPaymentSettings, null, null);
        }

        public PaymentProvider ProviderType
        {
            get
            {
                return PaymentProvider.Cmt;
            }
        }

        public PairingResponse Pair(Guid orderId, string cardToken, int? autoTipPercentage)
        {
            try
            {
                if (_serverSettings.GetPaymentSettings().PaymentMode == PaymentMethod.RideLinqCmt)
                {
                    var orderStatusDetail = _orderDao.FindOrderStatusById(orderId);
                    if (orderStatusDetail == null)
                    {
                        throw new Exception("Order not found");
                    }

                    if (orderStatusDetail.IBSOrderId == null)
                    {
                        throw new Exception("Order has no IBSOrderId");
                    }

                    var response = PairWithVehicleUsingRideLinq(orderStatusDetail, orderId, cardToken, autoTipPercentage);

                    // send a command to save the pairing state for this order
                    _commandBus.Send(new PairForPayment
                    {
                        OrderId = orderId,
                        Medallion = response.Medallion,
                        DriverId = response.DriverId.ToString(),
                        PairingToken = response.PairingToken,
                        PairingCode = response.PairingCode,
                        TokenOfCardToBeUsedForPayment = cardToken,
                        AutoTipPercentage = autoTipPercentage
                    });

                    return new PairingResponse
                    {
                        IsSuccessful = true,
                        Message = "Success",
                        PairingToken = response.PairingToken,
                        PairingCode = response.PairingCode,
                        Medallion = response.Medallion,
                        TripId = response.TripId,
                        DriverId = response.DriverId
                    };
                }

                _pairingService.Pair(orderId, cardToken, autoTipPercentage);

                return new PairingResponse
                {
                    IsSuccessful = true,
                    Message = "Success"
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                return new PairingResponse
                {
                    IsSuccessful = false,
                    Message = e.Message
                };
            }
        }

        public BasePaymentResponse Unpair(Guid orderId)
        {
            try
            {
                if (_serverSettings.GetPaymentSettings().PaymentMode == PaymentMethod.RideLinqCmt)
                {
                    var orderPairingDetail = _orderDao.FindOrderPairingById(orderId);
                    if (orderPairingDetail == null)
                    {
                        throw new Exception("Order not found");
                    }

                    UnpairFromVehicleUsingRideLinq(orderPairingDetail);

                    // send a command to delete the pairing pairing info for this order
                    _commandBus.Send(new UnpairForPayment
                    {
                        OrderId = orderId
                    });
                }

                _pairingService.Unpair(orderId);

                return new BasePaymentResponse
                {
                    IsSuccessful = true,
                    Message = "Success"
                };
            }
            catch (Exception e)
            {
                return new BasePaymentResponse
                {
                    IsSuccessful = false,
                    Message = e.Message
                };
            }
        }

        public void VoidPreAuthorization(Guid orderId)
        {
            // nothing to do for CMT since there's no notion of preauth
        }

        public void VoidTransaction(Guid orderId, string transactionId, ref string message)
        {
            var orderStatus = _orderDao.FindOrderStatusById(orderId);
            if (orderStatus == null)
            {
                throw new Exception("Order status not found");
            }

            Void(_serverSettings.GetPaymentSettings().CmtPaymentSettings.FleetToken,
                orderStatus.VehicleNumber,
                long.Parse(transactionId),
                orderStatus.DriverInfos == null 
                    ? 0 
                    : orderStatus.DriverInfos.DriverId.To<int>(),
                orderStatus.IBSOrderId.Value, ref message);
        }

        private void Void(string fleetToken, string deviceId, long transactionId, int driverId, int tripId, ref string message)
        {
            var reverseRequest = new ReverseRequest
            {
                FleetToken = fleetToken,
                DeviceId = deviceId,
                TransactionId = transactionId,
                DriverId = driverId,
                TripId = tripId
            };

            var responseReverseTask = _cmtPaymentServiceClient.PostAsync(reverseRequest);
            responseReverseTask.Wait();
            var reverseResponse = responseReverseTask.Result;
            _logger.LogMessage("CMT reverse response : " + reverseResponse.ResponseMessage);

            if (reverseResponse.ResponseCode != 1)
            {
                throw new Exception("Cannot cancel cmt transaction");
            }

            message = message + " The transaction has been cancelled.";
        }

        public DeleteTokenizedCreditcardResponse DeleteTokenizedCreditcard(string cardToken)
        {
            try
            {
                var responseTask = _cmtPaymentServiceClient.DeleteAsync(new TokenizeDeleteRequest
                {
                    CardToken = cardToken
                });
                responseTask.Wait();
                var response = responseTask.Result;

                return new DeleteTokenizedCreditcardResponse
                {
                    IsSuccessful = response.ResponseCode == 1,
                    Message = response.ResponseMessage
                };
            }
            catch (AggregateException ex)
            {
                ex.Handle(x =>
                {
                    _logger.LogError(x);
                    return true;
                });
                return new DeleteTokenizedCreditcardResponse
                {
                    IsSuccessful = false,
                    Message = ex.InnerExceptions.First().Message,
                };
            }
        }

        public PreAuthorizePaymentResponse PreAuthorize(Guid orderId, string email, string cardToken, decimal amountToPreAuthorize)
        {
            var paymentId = Guid.NewGuid();
            _commandBus.Send(new InitiateCreditCardPayment
            {
                PaymentId = paymentId,
                Amount = 0,
                Meter = 0,
                Tip = 0,
                TransactionId = string.Empty,
                OrderId = orderId,
                CardToken = cardToken,
                Provider = PaymentProvider.Cmt,
                IsNoShowFee = false
            });

            return new PreAuthorizePaymentResponse
            {
                IsSuccessful = true,
                Message = string.Empty
            };
        }

        public CommitPreauthorizedPaymentResponse CommitPayment(Guid orderId, decimal amount, decimal meterAmount, decimal tipAmount, string transactionId)
        {
            try
            {
                string authorizationCode = null;
                string commitTransactionId = transactionId;

                var orderDetail = _orderDao.FindById(orderId);
                if (orderDetail == null)
                {
                    throw new Exception("Order not found");
                }

                var orderStatus = _orderDao.FindOrderStatusById(orderId);
                if (orderStatus == null)
                {
                    throw new Exception("Order status not found");
                }

                var orderPayment = _paymentDao.FindByOrderId(orderId);
                if (orderPayment == null)
                {
                    throw new Exception("Order payment not found");
                }

                var deviceId = orderStatus.VehicleNumber;
                var driverId = orderStatus.DriverInfos == null ? 0 : orderStatus.DriverInfos.DriverId.To<int>();
                var employeeId = orderStatus.DriverInfos == null ? "" : orderStatus.DriverInfos.DriverId;
                var tripId = orderStatus.IBSOrderId.Value;
                var fleetToken = _serverSettings.GetPaymentSettings().CmtPaymentSettings.FleetToken;
                var customerReferenceNumber = orderStatus.ReferenceNumber.HasValue() ?
                                                    orderStatus.ReferenceNumber :
                                                    orderDetail.IBSOrderId.ToString();

                var authRequest = new AuthorizationRequest
                {
                    FleetToken = fleetToken,
                    DeviceId = deviceId,
                    Amount = (int)(amount * 100),
                    CardOnFileToken = orderPayment.CardToken,
                    CustomerReferenceNumber = customerReferenceNumber,
                    DriverId = driverId,
                    EmployeeId = employeeId,
                    ShiftUuid = orderDetail.Id.ToString(),
                    Fare = (int)(meterAmount * 100),
                    Tip = (int)(tipAmount * 100),
                    TripId = tripId,
                    ConvenienceFee = 0,
                    Extras = 0,
                    Surcharge = 0,
                    Tax = 0,
                    Tolls = 0
                };

                var responseTask = _cmtPaymentServiceClient.PostAsync(authRequest);
                responseTask.Wait();
                var authResponse = responseTask.Result;


                var isSuccessful = authResponse.ResponseCode == 1;
                if (isSuccessful)
                {
                    commitTransactionId = authResponse.TransactionId.ToString(CultureInfo.InvariantCulture);
                    authorizationCode = authResponse.AuthorizationCode;
                }

                return new CommitPreauthorizedPaymentResponse
                {
                    IsSuccessful = isSuccessful,
                    AuthorizationCode = authorizationCode,
                    Message = authResponse.ResponseMessage,
                    TransactionId = commitTransactionId
                };
            }
            catch (Exception ex)
            {
                return new CommitPreauthorizedPaymentResponse
                {
                    IsSuccessful = false,
                    TransactionId = transactionId,
                    Message = ex.Message
                };
            }
        }

        private CmtPairingResponse PairWithVehicleUsingRideLinq(OrderStatusDetail orderStatusDetail, Guid orderId, string cardToken, int? autoTipPercentage)
        {
            var accountDetail = _accountDao.FindById(orderStatusDetail.AccountId);

            // send pairing request                                
            var cmtPaymentSettings = _serverSettings.GetPaymentSettings().CmtPaymentSettings;
            var pairingRequest = new PairingRequest
            {
                AutoTipAmount = null,
                AutoTipPercentage = autoTipPercentage,
                AutoCompletePayment = true,
                CallbackUrl = "",
                CustomerId = orderStatusDetail.IBSOrderId.ToString(),
                CustomerName = accountDetail.Name,
                DriverId = orderStatusDetail.DriverInfos.DriverId,
                Latitude = orderStatusDetail.VehicleLatitude.GetValueOrDefault(),
                Longitude = orderStatusDetail.VehicleLongitude.GetValueOrDefault(),
                Medallion = orderStatusDetail.VehicleNumber,
                CardOnFileId = cardToken,
                Market = cmtPaymentSettings.Market
            };

            _logger.LogMessage("Pairing request : " + pairingRequest.ToJson());
            _logger.LogMessage("PaymentSettings request : " + cmtPaymentSettings.ToJson());

            var response = _cmtMobileServiceClient.Post(pairingRequest);

            _logger.LogMessage("Pairing response : " + response.ToJson());

            // wait for trip to be updated
            var watch = new Stopwatch();
            watch.Start();
            var trip = GetTrip(response.PairingToken);
            while (trip == null)
            {
                Thread.Sleep(2000);
                trip = GetTrip(response.PairingToken);

                if (watch.Elapsed.TotalSeconds >= response.TimeoutSeconds)
                {
                    _logger.LogMessage("Timeout Exception, Could not be paired with vehicle.");
                    throw new TimeoutException("Could not be paired with vehicle");
                }
            }

            return response;
        }

        private void UnpairFromVehicleUsingRideLinq(OrderPairingDetail orderPairingDetail)
        {
            // send unpairing request
            var response = _cmtMobileServiceClient.Delete(new UnpairingRequest
            {
                PairingToken = orderPairingDetail.PairingToken
            });

            // wait for trip to be updated
            var watch = new Stopwatch();
            watch.Start();
            var trip = GetTrip(orderPairingDetail.PairingToken);
            while (trip != null)
            {
                Thread.Sleep(2000);
                trip = GetTrip(orderPairingDetail.PairingToken);

                if (watch.Elapsed.TotalSeconds >= response.TimeoutSeconds)
                {
                    throw new TimeoutException("Could not be unpaired of vehicle");
                }
            }
        }

        private Trip GetTrip(string pairingToken)
        {
            try
            {
                var trip = _cmtMobileServiceClient.Get(new TripRequest { Token = pairingToken });
                if (trip != null)
                {
                    //ugly fix for deserilization problem in datetime on the device for IOS
                    if (trip.StartTime.HasValue)
                    {
                        trip.StartTime = DateTime.SpecifyKind(trip.StartTime.Value, DateTimeKind.Local);
                    }

                    if (trip.EndTime.HasValue)
                    {
                        trip.EndTime = DateTime.SpecifyKind(trip.EndTime.Value, DateTimeKind.Local);
                    }
                }

                return trip;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}