﻿#region

using System.Collections.Generic;
using System.Linq;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Common;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Enumeration;
using apcurium.MK.Common.Extensions;
using ServiceStack.CacheAccess;
using ServiceStack.ServiceInterface;

#endregion

namespace apcurium.MK.Booking.Api.Services
{
    public class ReferenceDataService : Service
    {
        public const string CacheKey = "IBS.StaticData";
        private readonly ICacheClient _cacheClient;
        private readonly IServerSettings _serverSettings;
        private readonly IIBSServiceProvider _ibsServiceProvider;

        public ReferenceDataService(
            IIBSServiceProvider ibsServiceProvider,
            ICacheClient cacheClient,
            IServerSettings serverSettings)
        {
            _ibsServiceProvider = ibsServiceProvider;
            _cacheClient = cacheClient;
            _serverSettings = serverSettings;
        }

        public object Get(ReferenceDataRequest request)
        {
            var cacheKey = string.Format("{0}{1}", CacheKey, request.CompanyKey);
            var result = _cacheClient.Get<ReferenceData>(cacheKey);

            if (result == null)
            {
                result = GetReferenceData(request.CompanyKey, request.Market);
                _cacheClient.Add(cacheKey, result);
            }

            if (!request.WithoutFiltering)
            {
                result.VehiclesList = FilterReferenceData(result.VehiclesList, _serverSettings.ServerData.IBS.ExcludedVehicleTypeId);
                result.CompaniesList = FilterReferenceData(result.CompaniesList, _serverSettings.ServerData.IBS.ExcludedProviderId);
            }

            var isChargeAccountPaymentEnabled = _serverSettings.GetPaymentSettings().IsChargeAccountPaymentEnabled;
            var isOutOfAppPaymentEnabled = _serverSettings.GetPaymentSettings().IsOutOfAppPaymentEnabled;

            IEnumerable<ListItem> filteredPaymentList = result.PaymentsList;

            if (!isChargeAccountPaymentEnabled)
            {
                filteredPaymentList = filteredPaymentList.Where(x => x.Id != ChargeTypes.Account.Id);
            }
            if (!isOutOfAppPaymentEnabled)
            {
                filteredPaymentList = filteredPaymentList.Where(x => x.Id != ChargeTypes.PaymentInCar.Id);
            }

            result.PaymentsList = filteredPaymentList.ToList();

            return result;
        }

        private ReferenceData GetReferenceData(string companyKey, string market)
        {
            var companies = _ibsServiceProvider.StaticData(companyKey, market).GetCompaniesList();
            var payments = new List<ListItem>();
            var vehicles = new List<ListItem>();

            foreach (var company in companies)
            {
                payments.AddRange(ChargeTypesClient.GetPaymentsList(company));
                vehicles.AddRange(_ibsServiceProvider.StaticData(companyKey, market).GetVehiclesList(company));
            }

            var equalityComparer = new ListItemEqualityComparer();
            var result = new ReferenceData
            {
                CompaniesList = companies.Distinct(equalityComparer).ToArray(),
                PaymentsList = payments.Distinct(equalityComparer).ToArray(),
                VehiclesList = vehicles.Distinct(equalityComparer).ToArray(),
            };

            return result;
        }

        private IList<ListItem> FilterReferenceData(IEnumerable<ListItem> reference, string excludedTypeId)
        {
            var excluded = excludedTypeId.IsNullOrEmpty()
                ? new int[0]
                : excludedTypeId.Split(';').Select(int.Parse).ToArray();

            return reference.Where(c => excluded.None(e => e == c.Id)).ToList();
        }

        private class ListItemEqualityComparer : EqualityComparer<ListItem>
        {
            public override bool Equals(ListItem x, ListItem y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return x.Id == y.Id;
            }

            public override int GetHashCode(ListItem obj)
            {
                if (obj == null) return 0;
                return obj.Id.GetHashCode();
            }
        }
    }
}