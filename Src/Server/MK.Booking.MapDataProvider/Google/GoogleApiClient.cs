﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Booking.MapDataProvider.Resources;
using apcurium.MK.Booking.MapDataProvider.Google.Resources;
using System.Threading.Tasks;
using apcurium.MK.Booking.MapDataProvider.Extensions;
using apcurium.MK.Common.Extensions;
using apcurium.MK.Common;

namespace apcurium.MK.Booking.MapDataProvider.Google
{
    public class GoogleApiClient : BaseServiceClient, IPlaceDataProvider, IDirectionDataProvider, IGeocoder
	{
		private const string PlaceDetailsServiceUrl = "https://maps.googleapis.com/maps/api/place/details/";
		private const string PlacesServiceUrl = "https://maps.googleapis.com/maps/api/place/search/";
		private const string PlacesAutoCompleteServiceUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/";
		private const string DirectionsServiceUrl = "https://maps.googleapis.com/maps/api/directions/";
		private const string GeocodeServiceUrl = "https://maps.googleapis.com/maps/api/geocode/";

	    private const int MaxNumberOfAttemps = 3;
	    private const int RetryDelay = 1000;
		const uint MaximumPageLength = 50;

		private readonly IAppSettings _settings;
		private readonly ILogger _logger;
		private readonly IGeocoder _fallbackGeocoder;

        public GoogleApiClient(IAppSettings settings, ILogger logger, IConnectivityService connectivityService, IGeocoder fallbackGeocoder = null)
            : base (connectivityService)
        {
            _logger = logger;
            _settings = settings;
			_fallbackGeocoder = fallbackGeocoder;
        }

        protected string PlacesApiKey
        {
            get { return _settings.Data.Map.PlacesApiKey; }
        }

	    private string GoogleCryptoKey
	    {
            get { return "y20g3ePo3ROg4mB-5Vh9C2SojLw=";  }
	    }

	    private string GoogleClientId
	    {
            get { return "gme-taxihailinc"; }
	    }

        public Task<GeoPlace[]> GetNearbyPlacesAsync(double? latitude, double? longitude, string languageCode, int radius, uint maximumNumberOfPlaces = MaximumPageLength, string pipedTypeList = null)
	    {
            if (maximumNumberOfPlaces == 0)
            {
                maximumNumberOfPlaces = MaximumPageLength;
            }

            pipedTypeList = pipedTypeList ?? new PlaceTypes(_settings.Data.GeoLoc.PlacesTypes).GetPipedTypeList();
            var client = GetClient(PlacesServiceUrl);

            var @params = new Dictionary<string, string>
            {
                {"key", PlacesApiKey},
                {"radius", radius.ToString(CultureInfo.InvariantCulture)},
                {"language", languageCode},
                {"types", pipedTypeList}
            };

            if (latitude != null && longitude != null)
            {
                @params.Add("location",
                    string.Join(",", latitude.Value.ToString(CultureInfo.InvariantCulture),
                        longitude.Value.ToString(CultureInfo.InvariantCulture)));
            }

            var resource = "json" + BuildQueryString(@params);

            #if DEBUG
            Console.WriteLine(PlacesServiceUrl + resource);
            #endif

            return HandleGoogleResultAsync(() => client.GetAsync<PlacesResponse>(resource), x => x.Results.Select(ConvertPlaceToGeoPlaces).ToArray(), new GeoPlace[0]);
        }

        public Task<GeoPlace[]> SearchPlacesAsync(double? latitude, double? longitude, string query, string languageCode, int radius)
	    {
            var client = GetClient(PlacesAutoCompleteServiceUrl);
            var resource = GetPlacesAutocompleteSearchRequest(latitude, longitude, query, languageCode, radius, "establishment");

            #if DEBUG
            Console.WriteLine(PlacesAutoCompleteServiceUrl + resource);
            #endif

            return HandleGoogleResultAsync(() => client.GetAsync<PredictionResponse>(resource), 
                x => ConvertPredictionToPlaces(x.predictions).ToArray(), new GeoPlace[0]);
        }

	    public async Task<GeoPlace> GetPlaceDetailAsync(string id)
	    {
            var client = GetClient(PlaceDetailsServiceUrl);
            var resource = GetPlaceDetailRequest(id);

            Func<PlaceDetailResponse, GeoPlace> selector = response => new GeoPlace
            {
                Id = id,
                Name = response.Result.Formatted_address,
                Address = ResourcesExtensions.ConvertGeoObjectToAddress(response.Result)
            };

            return await HandleGoogleResultAsync(() => client.GetAsync<PlaceDetailResponse>(resource), selector, new GeoPlace()).ConfigureAwait(false);
	    }

        public Task<GeoAddress> GetAddressDetailAsync(string id)
        {
            var client = GetClient(PlaceDetailsServiceUrl);
            var resource = GetPlaceDetailRequest(id);

            Func<PlaceDetailResponse, GeoAddress> selector = response => ResourcesExtensions.ConvertGeoObjectToAddress (response.Result);

            return HandleGoogleResultAsync(() => client.GetAsync<PlaceDetailResponse>(resource), selector, new GeoAddress());
        }

	    public GeoPlace[] GetNearbyPlaces(double? latitude, double? longitude, string languageCode, int radius, uint maximumNumberOfPlaces = MaximumPageLength, string pipedTypeList = null)
	    {
	        return GetNearbyPlacesAsync(latitude, longitude, languageCode, radius, maximumNumberOfPlaces, pipedTypeList).Result;
	    }

        public GeoPlace[] SearchPlaces(double? latitude, double? longitude, string query, string languageCode, int radius)
		{
		    return SearchPlacesAsync(latitude, longitude, query, languageCode, radius).Result;
		}

		public GeoPlace GetPlaceDetail(string id)
		{
		    return GetPlaceDetailAsync(id).Result;
		}

        public GeoAddress GetAddressDetail(string id)
        {
            return GetAddressDetailAsync(id).Result;
        }

        public async Task<GeoDirection> GetDirectionsAsync(double originLat, double originLng, double destLat, double destLng, DateTime? date)
        {
            var client = GetClient(DirectionsServiceUrl);
            var @params = new Dictionary<string, string>
            {
                {"origin", string.Format(CultureInfo.InvariantCulture, "{0},{1}", originLat, originLng)},
                {"destination", string.Format(CultureInfo.InvariantCulture, "{0},{1}", destLat, destLng)}
            };

            var resource = "json" + BuildQueryString(@params);

            var signedUrl = Sign(DirectionsServiceUrl + resource);

            #if DEBUG
            Console.WriteLine(signedUrl);
            #endif

            var result = new GeoDirection();
            try
            {
				var direction = await client.GetAsync<DirectionResult>(signedUrl).ConfigureAwait(false);
                if (direction.Status == ResultStatus.OVER_QUERY_LIMIT)
                {
                    // retry 2 more times

                    var attempts = 1;
                    var success = false;

                    while(!success && attempts < 3)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
						direction = await client.GetAsync<DirectionResult>(signedUrl).ConfigureAwait(false);
                        attempts++;
                        success = direction.Status == ResultStatus.OK;
                    }
                }

                if (direction.Status == ResultStatus.OK)
                {
                    var route = direction.Routes.ElementAt(0);
                    if (route.Legs.Count > 0)
                    {
                        result.Distance = route.Legs.Sum(leg => leg.Distance.Value);
                        result.Duration = route.Legs.Sum(leg => leg.Duration.Value);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }

            return result;
        }

        public GeoAddress[] GeocodeAddress(string query, string currentLanguage, double? pickupLatitude, double? pickupLongitude, double searchRadiusInMeters)
		{
            return GeocodeAddressAsync(query, currentLanguage, pickupLatitude, pickupLongitude, searchRadiusInMeters).Result;
		}
            
        public Task<GeoAddress[]> GeocodeAddressAsync(string query, string currentLanguage, double? pickupLatitude, double? pickupLongitude, double searchRadiusInMeters)
	    {
            var client = GetClient(PlacesAutoCompleteServiceUrl);
            var resource = GetPlacesAutocompleteSearchRequest(pickupLatitude, pickupLongitude, query, currentLanguage, (int)searchRadiusInMeters, "address");

            #if DEBUG
            Console.WriteLine(PlacesAutoCompleteServiceUrl + resource);
            #endif

            return HandleGoogleResultAsync(() => client.GetAsync<PredictionResponse>(resource), 
                x => ConvertPredictionToAddresses(x.predictions).ToArray(), new GeoAddress[0], 
                () => _fallbackGeocoder.GeocodeAddressAsync(query, currentLanguage, pickupLatitude, pickupLongitude, searchRadiusInMeters));
	    }

	    public GeoAddress[] GeocodeLocation(double latitude, double longitude, string currentLanguage)
	    {
	        var requestParameter = GenerateGeocodeLocationRequestParameter(latitude, longitude, currentLanguage);

	        return Geocode(requestParameter, () => _fallbackGeocoder.GeocodeLocation (latitude, longitude, currentLanguage));
	    }

	    public Task<GeoAddress[]> GeocodeLocationAsync(double latitude, double longitude, string currentLanguage)
	    {
            var requestParameter = GenerateGeocodeLocationRequestParameter(latitude, longitude, currentLanguage);

            return GeocodeAsync(requestParameter, () => _fallbackGeocoder.GeocodeLocationAsync(latitude, longitude, currentLanguage));
	    }

        private string GetPlaceDetailRequest(string placeId)
        {
            var @params = new Dictionary<string, string>
            {
                {"placeid", placeId},
                {"key", PlacesApiKey},
            };

            return "json" + BuildQueryString(@params);
        }

        private string GetPlacesAutocompleteSearchRequest(double? latitude, double? longitude, string query, string languageCode, int radius, string types)
        {
            var @params = new Dictionary<string, string>
            {
                {"key", PlacesApiKey},
                {"radius", radius.ToString(CultureInfo.InvariantCulture)},
                {"language", languageCode},
                {"types", types}
            };

            if (latitude != null && longitude != null)
            {
                @params.Add("location",
                    string.Join(",", latitude.Value.ToString(CultureInfo.InvariantCulture),
                        longitude.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (query != null)
            {
                @params.Add("input", query.Replace(' ', '+'));
            }

            return "json" + BuildQueryString(@params);
        }

        private string GenerateGeocodeLocationRequestParameter(double latitude, double longitude, string currentLanguage)
        {
            var @params = new Dictionary<string, string>
            {
                {"latlng", string.Format(CultureInfo.InvariantCulture, "{0},{1}", latitude, longitude)},
                {"language", currentLanguage}
            };

            var requestParameter = "json" + BuildQueryString(@params);
            return requestParameter;
        }

        private GeoAddress[] Geocode(string requestParameters, Func<GeoAddress[]> fallBackAction)
        {
            return GeocodeAsync(requestParameters, () => Task.Run(fallBackAction)).Result;
        }

	    private Task<GeoAddress[]> GeocodeAsync(string requestParameters, Func<Task<GeoAddress[]>> fallBackAction)
	    {
            var client = GetClient(GeocodeServiceUrl);

            var signedUrl = Sign(GeocodeServiceUrl + requestParameters);

            #if DEBUG
            Console.WriteLine(signedUrl);
            #endif

            return HandleGoogleResultAsync(() => client.GetAsync<GeoResult>(signedUrl), ResourcesExtensions.ConvertGeoResultToAddresses, new GeoAddress[0], fallBackAction);
	    }

        private string Sign(string url)
        {
            // add Google Client Id to query
            url += "&client=" + GoogleClientId;

            var encoding = new ASCIIEncoding();

            // converting key to bytes will throw an exception, need to replace '-' and '_' characters first.
            var usablePrivateKey = GoogleCryptoKey.Replace("-", "+").Replace("_", "/");
            var privateKeyBytes = Convert.FromBase64String(usablePrivateKey);

            var uri = new Uri(url);
            var encodedPathAndQueryBytes = encoding.GetBytes(uri.LocalPath + uri.Query);

            // compute the hash
            var algorithm = new HMACSHA1(privateKeyBytes);
            var hash = algorithm.ComputeHash(encodedPathAndQueryBytes);

            // convert the bytes to string and make url-safe by replacing '+' and '/' characters
            var signature = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_");

            // Add the signature to the existing URI.
            return uri.Scheme + "://" + uri.Host + uri.LocalPath + uri.Query + "&signature=" + signature;
        }

	    private async Task<TResponse> HandleGoogleResultAsync<TResponse, TGoogleResponse>(Func<Task<TGoogleResponse>> apiCall, Func<TGoogleResponse, TResponse> selector, TResponse defaultResult, Func<Task<TResponse>> fallBackAction = null)
	        where TGoogleResponse : GoogleResult
	    {
            try
            {
                var result = await apiCall().ConfigureAwait(false);

                if (result.Status == ResultStatus.OVER_QUERY_LIMIT)
                {
                    // retry 2 more times

                    var attempts = 1;
                    var success = false;

                    while (!success && attempts < MaxNumberOfAttemps)
                    {
                        await Task.Delay(RetryDelay);
                        result = await apiCall();
                        attempts++;
                        success = result.Status == ResultStatus.OK;
                    }
                }

                // if we still have OVER_QUERY_LIMIT or REQUEST_DENIED and a fallback geocoder, we invoke it
                if ((result.Status == ResultStatus.OVER_QUERY_LIMIT
                    || result.Status == ResultStatus.REQUEST_DENIED)
                        && _fallbackGeocoder != null
                        && fallBackAction != null)
                {
                    return await fallBackAction();
                }

                if (result.Status == ResultStatus.OK)
                {
                    return selector(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }

            return defaultResult;
	    }

		private GeoPlace ConvertPlaceToGeoPlaces(Place place)
		{            
			return new GeoPlace
            {
				Name =  place.Name,
                Id = place.Place_Id,                                                       
				Types = place.Types,
				Address = new GeoAddress
				{
				    FullAddress = place.Formatted_Address ?? place.Vicinity,
                    Latitude = place.Geometry.Location.Lat,
                    Longitude = place.Geometry.Location.Lng
				}
			};
		}

		private IEnumerable<GeoPlace> ConvertPredictionToPlaces(IEnumerable<Prediction> result)
        {            
            return result.Select(p => 
                new GeoPlace
                {
                    Id = p.Place_Id,
                    Name = GetNameFromDescription(p.Description),
                    Address = new GeoAddress { FullAddress =  GetAddressFromDescription(p.Description, false) },                                                       
                    Types = p.Types
                });
        }

        private IEnumerable<GeoAddress> ConvertPredictionToAddresses(IEnumerable<Prediction> result)
        {            
            return result.Select(p => 
                new GeoAddress 
                { 
                    Id = p.Place_Id,
                    FullAddress = GetAddressFromDescription(p.Description, true) 
                });
        }

        private string GetNameFromDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description) || !description.Contains(","))
            {
                return description;
            }
            var components = description.Split(',');
            return components.First().Trim();
        }

        private string GetAddressFromDescription(string description, bool isStreetAddress)
        {
            if (string.IsNullOrWhiteSpace(description) || !description.Contains(","))
            {
                return description;
            }
            var components = description.Split(',');
            if (components.Count() > 1)
            {
                return isStreetAddress
                    ? components.Select(c => c.Trim()).JoinBy(", ")
                    : components.Skip(1).Select(c => c.Trim()).JoinBy(", ");
            }
            return components.First().Trim();
        }

        private string BuildQueryString(IDictionary<string, string> @params)
        {
            return "?" + string.Join("&", @params.Select(x => string.Join("=", x.Key, x.Value)));
        }
    }
}