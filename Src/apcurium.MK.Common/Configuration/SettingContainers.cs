﻿using System.ComponentModel.DataAnnotations;
using apcurium.MK.Common.Configuration.Attributes;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Enumeration;
using apcurium.MK.Common.Services;

namespace apcurium.MK.Common.Configuration
{
    public class TaxiHailSettingContainer
    {
        [RequiredAtStartup, SendToClient, CustomizableByCompany]
        [Display(Name = "Configuration - Application Name", Description = "Application name as displayed in message")]
        public string ApplicationName { get; set; }

		[RequiredAtStartup, SendToClient]
		[Display(Name = "Configuration - ApplicationKey", Description = "(DO NOT MODIFY) Change Application Key ")]
        public string ApplicationKey { get; set; }

		[RequiredAtStartup]
        [Display(Name = "Email Setting - Accent Color", Description = "Email Border Color")]
        public string AccentColor { get; set; }

        [Display(Name = "Email Setting - Font Color", Description = "Normal text font color in email body")]
        public string EmailFontColor { get; set; }

        [Hidden]
        [Display(Name = "SiteName for IIS", Description = "SiteName")]
        public string SiteName { get; set; }
    }

    public class OrderStatusSettingContainer
    {
        [SendToClient, CustomizableByCompany]
        [Display(Name = "Configuration - Polling Interval Client", Description = "App status refresh interval when in a ride (in seconds)")]
        public int ClientPollingInterval { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Configuration - Polling Interval Server", Description = "Order status from sever polling interval to IBS (in seconds)")]
        public int ServerPollingInterval { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Configuration - Include PickupDate No Older Than Hours", Description = "Number of hours from the server date time to use wen considering an order to still be in progress (in hours)")]
        public int PickupDateNoOlderThan { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Configuration - Include PickupDate No Newer Than Hours", Description = "Number of hours from the server date time to use wen considering a future booking order to be actively in progress (in hours)")]
        public int PickupDateNoNewerThan { get; set; }

    }

    public class GCMSettingContainer
    {
		[PropertyEncrypt]
        [SendToClient]
        [Display(Name = "Notification - GCM Sender Id", Description = "Google Cloud Messaging (Push notification) Sender Id")]
        public string SenderId { get; set; }

		[PropertyEncrypt]
        [Display(Name = "Notification - GCM API Key", Description = "Google Cloud Messaging (Push notification) API Id")]
        public string APIKey { get; set; }

		[PropertyEncrypt]
        [Display(Name = "Notification - GCM Package Name", Description = "Google Cloud Messaging (Push notification) Package Name")]
        public string PackageName { get; set; }
    }

    public class DirectionSettingContainer
    {
        [SendToClient]
        [Display(Name = "Estimate - Tarif Mode", Description = "How the tarif (estimate) calculation is done: by the APP or by IBS")]
        public TarifMode TarifMode { get; set; }

        [Display(Name = "Estimate - Need a Valid Tarif", Description = "Prevent order when tarif (estimate) is not available")]
        public bool NeedAValidTarif { get; set; }

        [Display(Name = "Estimate - Flat Rate", Description = "Flat Rate for estimation")]
        public decimal FlateRate { get; set; }

        [Display(Name = "Estimate - Rate Per Km", Description = "Rate per km for estimation")]
        public double RatePerKm { get; set; }
    }

    public class NearbyPlacesServiceSettingContainer
    {
        [SendToClient]
        [Display(Name = "Search - Default Nearby Places Radius", Description = "Default radius (in meters) for nearby places search")]
        public int DefaultRadius { get; set; }
    }

    public class MapSettingContainer
    {
		[PropertyEncrypt]
        [Hidden]
        [SendToClient]
        [Display(Name = "Search - Places Api Key", Description = "Google Places Api Key")]
        public string PlacesApiKey { get; set; }
    }

    public class GeoLocSettingContainer
    {
        [RequiredAtStartup, SendToClient, CustomizableByCompany]
        [Display(Name = "Configuration - Default Latitude", Description = "Default latitude to display the map before geoloc is done")]
        public double DefaultLatitude { get; set; }

        [RequiredAtStartup, SendToClient, CustomizableByCompany]
        [Display(Name = "Configuration - Default Longitude", Description = "Default longitude to display the map before geoloc is done")]
        public double DefaultLongitude { get; set; }

        [SendToClient, CustomizableByCompany]
        [Display(Name = "Search - Place Types", Description = "Give a list of Google Maps places types to filter search")]
        public string PlacesTypes { get; set; }

        [RequiredAtStartup, SendToClient, CustomizableByCompany]
        [Display(Name = "Search - Search Filter", Description = "Filter for geolocation search")]
        public string SearchFilter { get; set; }

        [SendToClient, CustomizableByCompany]
        [Display(Name = "Search - Search Radius", Description = "Search radius (in meters) for geolocation search (only a hint to the search engine). (Default: 45000m)")]
        public int SearchRadius { get; set; }
    }

    public class AvailableVehiclesSettingContainer
    {
        [SendToClient, CustomizableByCompany]
        [Display(Name = "Available Vehicle - Enable", Description = "Available Vehicles feature is enabled")]
        public bool Enabled { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Available Vehicle - Max Vehicles Count", Description = "Maximum number of available vehicles to be shown")]
        public int Count { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Available Vehicle - Radius", Description = "Only available vehicle inside the radius (in meters) will be taken into account")]
        public int Radius { get; set; }
    }

    public class SmtpSettingContainer
    {
        public SmtpSettingContainer()
        {
            Credentials = new CredentialsContainer();
        }

		[PropertyEncrypt]
        [Display(Name = "Email Setting - SMTP Host", Description = "SMTP Host")]
        public string Host { get; set; }

        [Display(Name = "Email Setting - SMTP Port", Description = "SMTP Port")]
        public int Port { get; set; }

        [Display(Name = "Email Setting - SMTP Use Default Credentials", Description = "SMTP Use Default Credentials")]
        public bool UseDefaultCredentials { get; set; }

        [Display(Name = "Email Setting - SMTP Enable SSL", Description = "SMTP Enable SSL")]
        public bool EnableSsl { get; set; }

        [Display(Name = "Email Setting - SMTP Delivery Method", Description = "SMTP DeliveryMethod")]
        public SmtpDeliveryMethod DeliveryMethod { get; set; }

        public CredentialsContainer Credentials { get; set; }
    }

    public class CredentialsContainer
    {
		[PropertyEncrypt]
        [Display(Name = "Email Setting - SMTP Username", Description = "SMTP Username")]
        public string Username { get; set; }

		[PropertyEncrypt]
        [Display(Name = "Email Setting - SMTP Password", Description = "SMTP Password")]
        public string Password { get; set; }
    }

    public class APNSSettingContainer
    {
		[PropertyEncrypt]
        [Display(Name = "Notification - Certificate Password", Description = "Apple Push Notification Service Certificate Password")]
        public string CertificatePassword { get; set; }

        [Display(Name = "Notification - Development Certificate Path", Description = "Apple Push Notification Service Development Certificate Path")]
        public string DevelopmentCertificatePath { get; set; }

        [Display(Name = "Notification - Production Certificate Path", Description = "Apple Push Notification Service Production Certificate Path")]
        public string ProductionCertificatePath { get; set; }
    }

    public class BlackberrySettingContainer
    {
		[PropertyEncrypt]
        [Display(Name = "Notification - Blackberry App Id", Description = "Balckberry Notification Application Id")]
        public string AppId { get; set; }

		[PropertyEncrypt]
        [Display(Name = "Notification - Blackberry App Password", Description = "Balckberry Notification Password")]
        public string Password { get; set; }

		[PropertyEncrypt]
        [Display(Name = "Notification - Blackberry Url", Description = "Blackerry Send Url")]
        public string Url { get; set; }
    }

    public class DefaultBookingSettingsSettingContainer
    {
        [CustomizableByCompany]
        [Display(Name = "Account - Default value for Charge Type ID", Description = "Default value at account creation for charge type (1 = InCar, 2 = Account, 3 = CoF, 4 = Paypal)")]
        public int? ChargeTypeId { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Account - Default value for Nb of passenger", Description = "Default value at account creation for number of passenger")]
        public int NbPassenger { get; set; }

        [CustomizableByCompany]
        [Display(Name = "IBS - Provider Id", Description = "Must match IBS GetProviders call")]
        public int? ProviderId { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Account - Default value for Vehicle Type ID", Description = "Default value at account creation for vehicle type (0 = no preference)")]
        public int? VehicleTypeId { get; set; }
    }

	public class StoreSettingContainer
    {
		[SendToClient, CustomizableByCompany]
		[Display(Name = "Website - App store link", Description = "Link to Apple App Store (Must be HTTP)")]
		public string AppleLink { get; set; }

		[SendToClient, CustomizableByCompany]
		[Display(Name = "Website - Google Play store link", Description = "Link to Google Play Store (Must be HTTP)")]
		public string PlayLink { get; set; }
    }

    public class IBSSettingContainer
    {
        [Display(Name = "IBS - Auto Dispatch", Description = "IBS AutoDispatch")]
        public bool AutoDispatch { get; set; }

		[PropertyEncrypt]
        [Display(Name = "IBS - Default Account Password", Description = "IBS DefaultAccountPassword")]
        public string DefaultAccountPassword { get; set; }

        [Display(Name = "IBS - Time Difference", Description = "IBS Time Difference (calculated in ticks, 10 million ticks in a second)")]
        [CustomizableByCompany]
        public long TimeDifference { get; set; }

        [Display(Name = "IBS - Fake Order Status Update", Description = "Put IBS in fake mode")]
        public bool FakeOrderStatusUpdate { get; set; }

        [CustomizableByCompany]
        [Display(Name = "IBS - Driver Note Template", Description = "Driver Note Template")]
        public string NoteTemplate { get; set; }

        [CustomizableByCompany]
        [Display(Name = "IBS - Hide Charge Type In User Note", Description = "Controls if the charge type is sent to driver as part of the user note")]
        public bool HideChargeTypeInUserNote { get; set; }

        [Display(Name = "IBS - Order Priority", Description = "IBS OrderPriority")]
        public bool OrderPriority { get; set; }

        [Display(Name = "IBS - Excluded Provider Id", Description = "IBS ExcludedProviderId")]
        public string ExcludedProviderId { get; set; }

        [Display(Name = "IBS - Excluded Vehicle Type Id", Description = "IBS ExcludedVehicleTypeId")]
        public string ExcludedVehicleTypeId { get; set; }

        [Display(Name = "IBS - Zone By Company Enabled", Description = "IBS ZoneByCompanyEnabled")]
        public bool ZoneByCompanyEnabled { get; set; }

        [Display(Name = "IBS - Validate Destination Zone", Description = "IBS ValidateDestinationZone")]
        public bool ValidateDestinationZone { get; set; }

        [Display(Name = "IBS - Validate Pickup Zone", Description = "IBS ValidatePickupZone")]
        public bool ValidatePickupZone { get; set; }

        [Display(Name = "IBS - Destination Zone To Exclude", Description = "IBS DestinationZoneToExclude")]
        public string DestinationZoneToExclude { get; set; }

        [Display(Name = "IBS - Pickup Zone To Exclude", Description = "IBS PickupZoneToExclude")]
        public string PickupZoneToExclude { get; set; }

		[PropertyEncrypt]
        [RequiredAtStartup]
        [Display(Name = "IBS - Rest Api Url", Description = "IBS RestApiUrl")]
        public string RestApiUrl { get; set; }

		[PropertyEncrypt]
        [RequiredAtStartup]
        [Display(Name = "IBS - Rest Api User", Description = "IBS RestApiUser")]
        public string RestApiUser { get; set; }

		[PropertyEncrypt]
        [RequiredAtStartup]
        [Display(Name = "IBS - Rest Api Secret", Description = "IBS RestApiSecret")]
        public string RestApiSecret { get; set; }

		[PropertyEncrypt]
        [RequiredAtStartup]
        [Display(Name = "IBS - Web Services Url", Description = "IBS WebServicesUrl")]
        public string WebServicesUrl { get; set; }

		[PropertyEncrypt]
        [RequiredAtStartup]
        [Display(Name = "IBS - Web Services Username", Description = "IBS WebServicesUserName")]
        public string WebServicesUserName { get; set; }

		[PropertyEncrypt]
        [RequiredAtStartup]
        [Display(Name = "IBS - Web Services Password", Description = "IBS WebServicesPassword")]
        public string WebServicesPassword { get;  set; }

        [CustomizableByCompany]
        [Display(Name = "IBS - Payment Type Card On File Id", Description = "Set IBS Card On File ID")]
        public int? PaymentTypeCardOnFileId { get; set; }

        [CustomizableByCompany]
        [Display(Name = "IBS - Payment Type Payment In Car Id", Description = "Set IBS Payment Type Payment In Car Id")]
        public int? PaymentTypePaymentInCarId { get; set; }

        [CustomizableByCompany]
        [Display(Name = "IBS - Payment Type Charge Account Id", Description = "Set IBS Payment Type Charge Account Id")]
        public int? PaymentTypeChargeAccountId { get; set; }
    }

    public class EmailSettingContainer
    {
        [Display(Name = "Email Setting - No Reply Email", Description = "No Reply Email")]
        public string NoReply { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Email Setting - CC Email Address", Description = "Email address to put in CC when sending an email to a user (booking confirmation only for now)")]
        public string CC { get; set; }
    }

    public class ReceiptSettingContainer
    {
        [CustomizableByCompany]
        [Display(Name = "Email Setting - Receipt Note", Description = "Receipt note added at end of email receipt")]
        public string Note { get; set; }
    }

    public class CustomerPortalSettingContainer
    {
		[PropertyEncrypt]
        [Display(Name = "Network Setting - Customer Portal Url", Description = "Customer Portal Url")]
        public string Url { get; set; }

		[PropertyEncrypt]
        [Display(Name = "Network Setting - Customer Portal Username", Description = "Customer UserName")]
        public string UserName { get; set; }

		[PropertyEncrypt]
        [Display(Name = "Network Setting - Customer Portal Password", Description = "Customer Portal Password")]
        public string Password { get; set; }
    }

    public class NetworkSettingContainer
    {
        [CustomizableByCompany]
        [Display(Name = "Network Setting - Primary Order Timeout", Description = "Time (in seconds) before swithching to secondary fleet")]
        public double PrimaryOrderTimeout { get; set; }

        [CustomizableByCompany]
        [Display(Name = "Network Setting - Secondary Order Timeout", Description = "Time (in seconds) before timing out on order")]
        public double SecondaryOrderTimeout { get; set; }

        [CustomizableByCompany, SendToClient]
        [Display(Name = "Network Setting - Hide Market Change Warning", Description = "Hide the warning that is displayed when entering a new market")]
        public bool HideMarketChangeWarning { get; set; }

        [CustomizableByCompany, SendToClient]
        [Display(Name = "Network Setting - Auto Confirm Fleet Change", Description = "Automatically change company if timeout occurs when trying to assing a taxi")]
        public bool AutoConfirmFleetChange { get; set; }

        [SendToClient]
        [Display(Name = "Network Setting - Enable Network", Description = "Is TaxiHailNetwork Enabled")]
        public bool Enabled { get;  set; }
    }

    public class HoneyBadgerSettingContainer
    {
		[PropertyEncrypt]
        [Display(Name = "Available Vehicle - Honey Badger service URL", Description = "Honey Badger Service Url. N.B.: Market request parameter is added automatically by the middleware, no need to add it here.")]
        public string ServiceUrl { get; set; }

        [Display(Name = "Available Vehicle - Honey Badger Market", Description = "Market used to find vehicles when Available Vehicles Mode is set to 'HoneyBadger'")]
        public string AvailableVehiclesMarket { get; set; }

        [Display(Name = "Available Vehicle - Honey Badger Fleet ID", Description = "Fleet ID used to find vehicles when Available Vehicles Mode is set to 'HoneyBadger'")]
        public int? AvailableVehiclesFleetId { get; set; }
    }

    public class CmtGeoSettingContainer
    {
		[PropertyEncrypt]
        [Display(Name = "Available Vehicle - CMT Geo Service URL", Description = "Cmt geo Service Url. N.B.: Market request parameter is added automatically by the middleware, no need to add it here.")]
        public string ServiceUrl { get; set; }

		[PropertyEncrypt]
        [Hidden]
        [Display(Name = "Available Vehicle - CMT Geo API Key", Description = "The API key for geo services")]
        public string AppKey { get; set; }

        [Display(Name = "Available Vehicle - CMT Geo Market", Description = "Market used to find vehicles when Available Vehicles Mode is set to 'Geo'")]
        public string AvailableVehiclesMarket { get; set; }

        [Display(Name = "Available Vehicle - CMT Geo Fleet ID", Description = "Fleet ID used to find vehicles when Available Vehicles Mode is set to 'Geo'")]
        public int? AvailableVehiclesFleetId { get; set; }
    }

	public class FlightStatsSettingsContainer
	{
		[PropertyEncrypt]
		[Display(Name = "FlightStats - Application Id", Description = "Application id for flight stats API")]
		public string AppId { get; set; }

		[PropertyEncrypt]
		[Display(Name = "FlightStats - Application key", Description = "Application keys for flight stats API.")]
		public string ApplicationKeys { get; set; }

		[PropertyEncrypt]
		[Display(Name = "FlightStats - Api Url", Description = "Url to access the FlightStats api.")]
		public string ApiUrl { get; set; }

		[SendToClient, CustomizableByCompany]
		[Display(Name = "FlightStats - Use airport details screen", Description = "Display the airport details screen before the order review screen to allow the user to send airport related information to the driver.")]
		public bool UseAirportDetails { get; set; }
	}
}