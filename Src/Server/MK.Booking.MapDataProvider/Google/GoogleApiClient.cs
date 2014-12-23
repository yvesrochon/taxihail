﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Diagnostic;
using ServiceStack.ServiceClient.Web;
using apcurium.MK.Booking.MapDataProvider.Resources;
using apcurium.MK.Booking.MapDataProvider.Google.Resources;
using System.Threading.Tasks;
using apcurium.MK.Booking.MapDataProvider.Extensions;
using apcurium.MK.Common.Extensions;
using System.Threading;

namespace apcurium.MK.Booking.MapDataProvider.Google
{
	public class GoogleApiClient : IPlaceDataProvider, IDirectionDataProvider, IGeocoder
	{
		private const string PlaceDetailsServiceUrl = "https://maps.googleapis.com/maps/api/place/details/";
		private const string PlacesServiceUrl = "https://maps.googleapis.com/maps/api/place/search/";
		private const string PlacesAutoCompleteServiceUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/";
		private const string MapsServiceUrl = "https://maps.googleapis.com/maps/api/";
		private readonly IAppSettings _settings;
		private readonly ILogger _logger;
		private readonly IGeocoder _fallbackGeocoder;

        public GoogleApiClient(IAppSettings settings, ILogger logger, IGeocoder fallbackGeocoder = null)
        {
            _logger = logger;
            _settings = settings;
			_fallbackGeocoder = fallbackGeocoder;
        }

        protected string PlacesApiKey
        {
            get { return _settings.Data.Map.PlacesApiKey; }
        }

        protected string GoogleMapKey
        {
            get { return _settings.Data.GoogleMapKey; }
        }

		public GeoPlace[] GetNearbyPlaces(double? latitude, double? longitude, string languageCode, bool sensor, int radius, string pipedTypeList = null)
        {
            pipedTypeList = pipedTypeList ?? new PlaceTypes(_settings.Data.GeoLoc.PlacesTypes).GetPipedTypeList();
            var client = new JsonServiceClient(PlacesServiceUrl);

            var @params = new Dictionary<string, string>
            {
                {"sensor", sensor.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()},
                {"key", PlacesApiKey},
                {"radius", radius.ToString(CultureInfo.InvariantCulture)},
                {"language", languageCode},
                {"types", pipedTypeList},
            };

            if (latitude != null && longitude != null)
            {
                @params.Add("location",
                    string.Join(",", latitude.Value.ToString(CultureInfo.InvariantCulture),
                        longitude.Value.ToString(CultureInfo.InvariantCulture)));
            }

            var resource = "json" + BuildQueryString(@params);

            _logger.LogMessage("Nearby Places API : " + PlacesServiceUrl + resource);

            return HandleGoogleResult(() => client.Get<PlacesResponse>(resource), x => x.Results.Select(ConvertPlaceToGeoPlaces).ToArray(), new GeoPlace[0]);
        }

		public GeoPlace[] SearchPlaces(double? latitude, double? longitude, string name, string languageCode, bool sensor, int radius, string countryCode)
        {
            var client = new JsonServiceClient(PlacesAutoCompleteServiceUrl);

            var @params = new Dictionary<string, string>
            {
                {"sensor", sensor.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()},
                {"key", PlacesApiKey},
                {"radius", radius.ToString(CultureInfo.InvariantCulture)},
                {"language", languageCode},
                {"types", "establishment"},
                {"components", "country:" + countryCode},
            };

            if (latitude != null && longitude != null)
            {
                @params.Add("location",
                    string.Join(",", latitude.Value.ToString(CultureInfo.InvariantCulture),
                        longitude.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (name != null)
            {
                @params.Add("input", name);
            }

            var resource = "json" + BuildQueryString(@params);

            _logger.LogMessage("Search Places API : " + PlacesAutoCompleteServiceUrl + resource);

            return HandleGoogleResult(() => client.Get<PredictionResponse>(resource), x => ConvertPredictionToPlaces(x.predictions).ToArray(), new GeoPlace[0]); 
        }

		public GeoPlace GetPlaceDetail(string id)
        {
            var client = new JsonServiceClient(PlaceDetailsServiceUrl);
            var @params = new Dictionary<string, string>
            {
				{"placeid", id},
                {"sensor", true.ToString().ToLower()},
                {"key", PlacesApiKey},
            };

            var resource = "json" + BuildQueryString(@params);
            Console.WriteLine(resource);

            Func<PlaceDetailResponse, GeoPlace> selector = response => new GeoPlace 
            {
                Id = id,
                Name = response.Result.Formatted_address,
                Address = ResourcesExtensions.ConvertGeoObjectToAddress (response.Result)
            };

            return HandleGoogleResult(() => client.Get<PlaceDetailResponse>(resource), selector, new GeoPlace());
        }

        public async Task<GeoDirection> GetDirectionsAsync(double originLat, double originLng, double destLat, double destLng, DateTime? date)
        {
            var client = new JsonServiceClient(MapsServiceUrl);
            var resource = string.Format(CultureInfo.InvariantCulture,
                "directions/json?origin={0},{1}&destination={2},{3}&sensor=true", originLat, originLng, destLat, destLng);

            if (_settings.Data.ShowEta && GoogleMapKey.HasValue())
            {
                //eta requires a Google Map Key for Business server-side when sending the value to the driver
                resource += "&key=" + GoogleMapKey;
            }

            var result = new GeoDirection();
            try
            {
                var direction = await client.GetAsync<DirectionResult>(resource).ConfigureAwait(false);
                if (direction.Status == ResultStatus.OVER_QUERY_LIMIT)
                {
                    // retry 2 more times

                    var attempts = 1;
                    var success = false;

                    while(!success && attempts < 3)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        direction = await client.GetAsync<DirectionResult>(resource).ConfigureAwait(false);
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

		public GeoAddress[] GeocodeAddress(string address, string currentLanguage)
        {
			var resource = string.Format(CultureInfo.InvariantCulture, "geocode/json?address={0}&sensor=true&language={1}", address, currentLanguage);
            return Geocode(resource, () => _fallbackGeocoder.GeocodeAddress (address, currentLanguage));
        }

		public GeoAddress[] GeocodeLocation(double latitude, double longitude, string currentLanguage)
        {
			var resource = string.Format(CultureInfo.InvariantCulture,"geocode/json?latlng={0},{1}&sensor=true&language={2}", latitude, longitude, currentLanguage);
            return Geocode(resource, () => _fallbackGeocoder.GeocodeLocation (latitude, longitude, currentLanguage));
        }

        private GeoAddress[] Geocode(string resource, Func<GeoAddress[]> fallBackAction)
        {
            var client = new JsonServiceClient(MapsServiceUrl);

            if (GoogleMapKey.HasValue())
            {
                resource += "&key=" + GoogleMapKey;
            }

            _logger.LogMessage("GeocodeLocation : " + MapsServiceUrl + resource);

            return HandleGoogleResult(() => client.Get<GeoResult>(resource), ResourcesExtensions.ConvertGeoResultToAddresses, new GeoAddress [0], fallBackAction);
        }

        private TResponse HandleGoogleResult<TResponse, TGoogleResponse>(Func<TGoogleResponse> apiCall, Func<TGoogleResponse, TResponse> selector, TResponse defaultResult, Func<TResponse> fallBackAction = null)
            where TGoogleResponse : GoogleResult
        {
            try
            {
                var result = apiCall.Invoke();

                if (result.Status == ResultStatus.OVER_QUERY_LIMIT)
                {
                    // retry 2 more times

                    var attempts = 1;
                    var success = false;

                    while(!success && attempts < 3)
                    {
                        Thread.Sleep(1000);
                        result = apiCall.Invoke();
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
                    return fallBackAction.Invoke();
                }

                if (result.Status == ResultStatus.OK) 
                {
                    return selector.Invoke(result);
                } 
            }
            catch(Exception ex)
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
                    Address = new GeoAddress { FullAddress =  GetAddressFromDescription(p.Description) },                                                       
                    Types = p.Types
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

        private string GetAddressFromDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description) || !description.Contains(","))
            {
                return description;
            }
            var components = description.Split(',');
            if (components.Count() > 1)
            {
                return components.Skip(1).Select(c => c.Trim()).JoinBy(", ");
            }
            return components.First().Trim();
        }

        private string BuildQueryString(IDictionary<string, string> @params)
        {
            return "?" + string.Join("&", @params.Select(x => string.Join("=", x.Key, x.Value)));
        }
    }
}