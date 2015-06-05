using System.Linq;
using System.Threading.Tasks;
using apcurium.MK.Booking.MapDataProvider.CraftyClicks.Resources;
using apcurium.MK.Booking.MapDataProvider.Extensions;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Extensions;
using ServiceStack.ServiceClient.Web;

namespace apcurium.MK.Booking.MapDataProvider.CraftyClicks
{
    public class CraftyClicksService : IPostalCodeService
    {
        private readonly IAppSettings _settingsService;
        public CraftyClicksService(IAppSettings settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<Address[]> GetAddressFromPostalCodeAsync(string postalCode)
        {
            var client = GetClient();

            var addressInformation = await client.PostAsync(new CraftyClicksRequest
            {
                Postcode = postalCode,
                Key = _settingsService.Data.CraftyClicksApiKey
            });

			return ProcessAddressInformation(postalCode, addressInformation);
        }

        private Address[] ProcessAddressInformation(string postalCode, CraftyClicksAddress addressInformation)
        {
            if (addressInformation == null || addressInformation.Delivery_points == null)
            {
                return new Address[0];
            }

            return addressInformation.Delivery_points
                .Select(point => new Address()
                {
                    City = addressInformation.Town,
                    ZipCode = postalCode,
                    AddressLocationType = AddressLocationType.Unspeficied,
                    FullAddress =
                        GenerateFullAddress(point.Line_1, point.Line_2, addressInformation.Town, addressInformation.Postcode),
                    Latitude = addressInformation.GeoCode.Lat,
                    Longitude = addressInformation.GeoCode.Lng,
                    FriendlyName = point.Line_1,
                    AddressType = "craftyclicks"
                })
                .ToArray();
        }

        public Address[] GetAddressFromPostalCode(string postalCode)
        {
            var client = GetClient();

            var addressInformation = client.Post(new CraftyClicksRequest
            {
                Postcode = postalCode,
                Key = _settingsService.Data.CraftyClicksApiKey
            });

            return ProcessAddressInformation(postalCode, addressInformation);
        }

        public bool IsValidPostCode(string postalCode)
        {
            if (!postalCode.HasValue())
            {
                return false;
            }

            var postCodeTrimmed = postalCode.Replace(" ", "");

            return postCodeTrimmed.Length >= 5 && postCodeTrimmed.Length <=7;
        }


        private JsonServiceClient GetClient()
        {
#if DEBUG
            return new JsonServiceClient("http://pcls1.craftyclicks.co.uk/json/");
#else
            return new JsonServiceClient("https://pcls1.craftyclicks.co.uk/json/");
#endif
        }

        private string GenerateFullAddress(string line1, string line2, string town, string postcode)
        {
            return line2.HasValue() 
                ? string.Join(", ", line1, line2, town, postcode) 
                : string.Join(", ", line1, town, postcode);
        }
    }
}