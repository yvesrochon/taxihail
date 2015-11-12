﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using apcurium.MK.Booking.Data;
using apcurium.MK.Booking.Helpers;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Booking.Jobs;
using apcurium.MK.Booking.ReadModel.Query.Contract;
using apcurium.MK.Common;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Entity;
using AutoMapper;

namespace apcurium.MK.Booking.Services.Impl
{
    public class IbsCreateOrderService : IIbsCreateOrderService
    {
        private readonly IServerSettings _serverSettings;
        private readonly IVehicleTypeDao _vehicleTypeDao;
        private readonly IAccountDao _accountDao;
        private readonly ILogger _logger;
        private readonly IIBSServiceProvider _ibsServiceProvider;
        private readonly IUpdateOrderStatusJob _updateOrderStatusJob;
        private readonly IDispatcherService _dispatcherService;

        public IbsCreateOrderService(IServerSettings serverSettings,
            IVehicleTypeDao vehicleTypeDao,
            IAccountDao accountDao,
            ILogger logger,
            IIBSServiceProvider ibsServiceProvider,
            IUpdateOrderStatusJob updateOrderStatusJob,
            IDispatcherService dispatcherService)
        {
            _serverSettings = serverSettings;
            _vehicleTypeDao = vehicleTypeDao;
            _accountDao = accountDao;
            _logger = logger;
            _ibsServiceProvider = ibsServiceProvider;
            _updateOrderStatusJob = updateOrderStatusJob;
            _dispatcherService = dispatcherService;
        }

        public IBSOrderResult CreateIbsOrder(Guid accountId, Guid orderId, Address pickupAddress, Address dropOffAddress, string accountNumberString, string customerNumberString,
            string companyKey, int ibsAccountId, string name, string phone, int passengers, int? vehicleTypeId, string ibsInformationNote,
            DateTime pickupDate, string[] prompts, int?[] promptsLength, IList<ListItem> referenceDataCompanyList, string market, int? chargeTypeId,
            int? requestProviderId, Fare fare, double? tipIncentive, bool isHailRequest = false, int? companyFleetId = null)
        {
            if (_serverSettings.ServerData.IBS.FakeOrderStatusUpdate)
            {
                // Fake IBS order id
                return new IBSOrderResult
                {
                    OrderKey = new OrderKey
                    {
                        IbsOrderId = new Random(Guid.NewGuid().GetHashCode()).Next(90000, 90000000),
                        TaxiHailOrderId = orderId
                    }
                };
            }

            var defaultVehicleType = _vehicleTypeDao.GetAll().FirstOrDefault();
            var ibsOrderParams = IbsHelper.PrepareForIbsOrder(_serverSettings.ServerData.IBS, defaultVehicleType, chargeTypeId, pickupAddress, dropOffAddress, accountNumberString,
                customerNumberString, referenceDataCompanyList, market, requestProviderId);

            var dispatcherSettings = _dispatcherService.GetSettings(market, isHailRequest: isHailRequest);

            if (dispatcherSettings.NumberOfOffersPerCycle == 0)
            {
                // IBS is handling the dispatch
                var orderResult = _ibsServiceProvider.Booking(companyKey).CreateOrder(
                    orderId,
                    ibsOrderParams.ProviderId,
                    ibsAccountId,
                    name,
                    phone,
                    passengers,
                    vehicleTypeId,
                    ibsOrderParams.IbsChargeTypeId,
                    ibsInformationNote,
                    pickupDate,
                    ibsOrderParams.IbsPickupAddress,
                    ibsOrderParams.IbsDropOffAddress,
                    accountNumberString,
                    ibsOrderParams.CustomerNumber,
                    prompts,
                    promptsLength,
                    ibsOrderParams.DefaultVehicleTypeId,
                    tipIncentive,
                    dispatcherSettings.DurationOfOfferInSeconds,
                    fare);

                return Mapper.Map<IBSOrderResult>(orderResult);
            }

            return _dispatcherService.Dispatch(accountId, orderId, ibsOrderParams, new BestAvailableCompany { CompanyKey = companyKey, FleetId = companyFleetId},
                dispatcherSettings, accountNumberString, ibsAccountId, name, phone, passengers,
                vehicleTypeId, ibsInformationNote, pickupDate, prompts, promptsLength, market, fare, tipIncentive, isHailRequest);
        }

        public void CancelIbsOrder(int? ibsOrderId, string companyKey, string phone, Guid accountId)
        {
            var ibsAccountId = _accountDao.GetIbsAccountId(accountId, companyKey);
            if (ibsAccountId.HasValue)
            {
                _dispatcherService.CancelIbsOrder(ibsOrderId, companyKey, phone, ibsAccountId.Value);
            }
        }

        public void UpdateOrderStatusAsync(Guid orderId)
        {
            _logger.LogMessage(string.Format("Starting status updater for order {0}", orderId));

            new TaskFactory().StartNew(() => _updateOrderStatusJob.CheckStatus(orderId));
        }
    }
}
