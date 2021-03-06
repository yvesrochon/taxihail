using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Common.Extensions;
using apcurium.MK.Common;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Diagnostic;

#if CLIENT
using MK.Common.Exceptions;
using System.Net.Http;
#else
using apcurium.MK.Booking.Api.Client.Extensions;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Common.Web;
#endif

namespace apcurium.MK.Booking.Api.Client.TaxiHail
{
    public class CompanyServiceClient : BaseServiceClient
    {
        private const string CacheKey_Terms = "Terms";
        private const string CacheKey_TermsVersion = "TermsVersion";

        private readonly ICacheService _cacheService;

        public CompanyServiceClient(string url, string sessionId, IPackageInfo packageInfo, ICacheService cacheService, IConnectivityService connectivityService, ILogger logger)
            : base(url, sessionId, packageInfo, connectivityService, logger)
        {
            _cacheService = cacheService;
        }

#if CLIENT
        private void HandleResponseHeader(HttpResponseMessage response)
        {
			try 
			{
				var version = response.Headers.GetValues("ETag").FirstOrDefault();

				if (version.HasValueTrimmed())
				{
					//put in the cache the etag
					_cacheService.Set(CacheKey_TermsVersion, version);
				}
			}
			catch
			{
			}
        }

        private void AddVersionInformation(HttpClient client)
        {
            //get the etag from the cache and add it to the headers
            var version = _cacheService.Get<string>(CacheKey_TermsVersion);
            if (version == null)
            {
                return;
            }

            if (client.DefaultRequestHeaders.Any(header => header.Key == "If-None-Match"))
            {
                client.DefaultRequestHeaders.Remove("If-None-Match");
            }

            client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", version);
        }
#else
        private void HandleResponseHeader(HttpWebResponse response)
        {
			try 
			{
				var version = response.Headers.GetValues(HttpHeaders.ETag).FirstOrDefault();
				if (version.HasValueTrimmed())
				{
					//put in the cache the etag
					_cacheService.Set(CacheKey_TermsVersion, version);
				}
			}
			catch 
			{
			}
        }
        private void AddVersionInformation(HttpWebRequest request)
        {
            //get the etag from the cache and add it to the headers
            var version = _cacheService.Get<string>(CacheKey_TermsVersion);
            if (version != null)
            {
                request.Headers.Set(HttpHeaders.IfNoneMatch, version);
            }
        }
#endif
        public async Task<TermsAndConditions> GetTermsAndConditions()
        {
            try
            {
                TermsAndConditions termsAndConditions;
#if CLIENT
                using(var client = Client)
                {
                    AddVersionInformation(client);
                    termsAndConditions = await client.GetAsync<TermsAndConditions>("/termsandconditions", HandleResponseHeader);
                }
#else
                Client.LocalHttpWebRequestFilter += AddVersionInformation;
                Client.LocalHttpWebResponseFilter += HandleResponseHeader;
                termsAndConditions = await Client.GetAsync<TermsAndConditions>("/termsandconditions");
#endif
                _cacheService.Set(CacheKey_Terms, termsAndConditions);

                return termsAndConditions;
            }
            catch (Exception ex)
            {
                var result = HandleException(ex);

                if (result == null)
                {
                    throw;
                }

                return result;
            }
        }
			
        private TermsAndConditions HandleException(Exception error)
        {
            var webServiceError = error as WebServiceException;

            if (webServiceError == null || webServiceError.StatusCode != (int) HttpStatusCode.NotModified)
            {
                return null;
            }

            //get the object from the cache and deserialize
            var terms = _cacheService.Get<TermsAndConditions>(CacheKey_Terms);

            if (terms == null)
            {
                return null;
            }

            terms.Updated = false;
            return terms;
        }

        public Task<AccountCharge> GetAccountCharge(string accountNumber, string customerNumber)
        {
#if CLIENT
                const bool hideAnswers = true;
#else
                const bool hideAnswers = false;
#endif

            var request = string.Format("/admin/accountscharge/{0}/{1}/{2}", accountNumber, customerNumber, hideAnswers);
            return Client.GetAsync<AccountCharge>(request, logger: Logger);
        }

        public Task<NotificationSettings> GetNotificationSettings()
        {
            return Client.GetAsync<NotificationSettings>("/settings/notifications", logger: Logger);
        }

        public Task<ActivePromotion[]> GetActivePromotions()
        {
            return Client.GetAsync(new ActivePromotions(), Logger);
        }
    }
}