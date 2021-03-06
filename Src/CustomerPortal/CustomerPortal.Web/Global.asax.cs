﻿#region

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Extensions;
using AutoMapper;
using CustomerPortal.Contract.Resources;
using CustomerPortal.Web.Entities;
using CustomerPortal.Web.Entities.Network;
using CustomerPortal.Web.Extensions;
using CustomerPortal.Web.Services;
using CustomerPortal.Web.Services.Impl;
using log4net.Config;
using MongoRepository;
using Newtonsoft.Json;
using WebMatrix.WebData;

#endregion

namespace CustomerPortal.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            XmlConfigurator.Configure(new FileInfo(Server.MapPath("~/log4net.xml")));

            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AutoMapperConfig.RegisterMaps();

            var username = "taxihail@apcurium.com";
            var roles = Roles.GetAllRoles();
            if (!roles.Any(x => x == RoleName.Admin))
            {
                Roles.CreateRole(RoleName.Admin);
            }
            if (!roles.Any(x => x == RoleName.Customer))
            {
                Roles.CreateRole(RoleName.Customer);
            }
            var admin = Membership.GetUser(username);
            if (admin == null)
            {
                WebSecurity.CreateUserAndAccount(username, "apcurium5200!");
            }
            if (!Roles.IsUserInRole(username, RoleName.Admin))
            {
                Roles.AddUserToRole(username, RoleName.Admin);
            }
            if (!Roles.IsUserInRole(username, RoleName.Customer))
            {
                Roles.AddUserToRole(username, RoleName.Customer);
            }
            
            GlobalConfiguration.Configuration.EnsureInitialized();


            DeleteTempFiles();


            //EnsureEnvironnementsAreInit();
            EnsureMenuColorIsSet();
            EnsureDefaultDevicesAreInit();
            EnsureDefaultSettingsAreInit();

            MigrateNetworkVehiclesToMarketRepresentation();
            MigrateCompanyRegionToMarketRegion();

            Mapper.CreateMap<EmailSender.SmtpConfiguration, SmtpClient>()
                .ForMember(x => x.Credentials, opt => opt.MapFrom(x => new NetworkCredential(x.Username, x.Password)));

            var enableWatchDog = (bool)new AppSettingsReader().GetValue("EnableWatchDog", typeof(bool));
            if (enableWatchDog)
            {
                StartStatusUpdater(TimeSpan.FromMilliseconds(100));
            }
        }
        
        private static bool _statusUpdaterRunning;
        private static readonly ILogger Logger = new Logger();
        private static readonly IServiceStatusUpdater UpdaterService = new ServiceStatusUpdater(Logger);
        private static readonly SerialDisposable Subscriptions = new SerialDisposable();

        private static void StartStatusUpdater(TimeSpan startTime)
        {
            Subscriptions.Disposable = Observable.Timer(startTime, TimeSpan.FromMinutes(3))
                .Where(_ => !_statusUpdaterRunning)
                .Do(_ =>
                {
                    _statusUpdaterRunning = true;
                    Logger.LogMessage("Company status updater started");
                })
                .Do(_ =>
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                    try
                    {
                        UpdaterService.UpdateServiceStatus(cts.Token);
                        cts.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }
                })
                .Do(_ =>
                {
                    Logger.LogMessage("Company status updater ended");
                    _statusUpdaterRunning = false;
                })
                .Subscribe(_ => { }, ex =>
                {
                    Logger.LogError(ex);

                    //Restarting StatusUpdater in 3 minutes
                    StartStatusUpdater(TimeSpan.FromMinutes(3));
                });
        }

        private static void MigrateCompanyRegionToMarketRegion()
        {
            var thNetworkSettingsRepo = new MongoRepository<TaxiHailNetworkSettings>();
            
            var allTaxiHailNetworkSettingsNotMigrated = thNetworkSettingsRepo
                .Where(x => x.Region != null).ToList();

            if (allTaxiHailNetworkSettingsNotMigrated.None())
            {
                Logger.LogMessage("All compagnies migrated");
                return;
            }

            var companiesByMarket = allTaxiHailNetworkSettingsNotMigrated
                .GroupBy(x => x.Market, taxihailNetworkSettings => taxihailNetworkSettings);

            var marketRepo = new MongoRepository<Market>();

            foreach (var grouping in companiesByMarket.OrderBy(x => x.Key))
            {
                Logger.LogMessage("Market: {0}", grouping.Key);

                var eligibleNetworkSettings = grouping.Where(x => x.IsInNetwork).ToList();
                var allLats = eligibleNetworkSettings.SelectMany(x => new [] { x.Region.CoordinateStart.Latitude, x.Region.CoordinateEnd.Latitude }).Distinct().ToList();
                var allLngs = eligibleNetworkSettings.SelectMany(x => new[] { x.Region.CoordinateStart.Longitude, x.Region.CoordinateEnd.Longitude }).Distinct().ToList();

                if (allLngs.Any() && allLngs.Any())
                {
                    var lowerLeftForMarket = new MapCoordinate { Latitude = allLats.Min(x => x), Longitude = allLngs.Min(x => x) };
                    var upperRightForMarket = new MapCoordinate { Latitude = allLats.Max(x => x), Longitude = allLngs.Max(x => x) };

                    var market = marketRepo.GetMarket(grouping.Key);
                    market.Region = new MapRegion { CoordinateStart = lowerLeftForMarket, CoordinateEnd = upperRightForMarket };
                    marketRepo.Update(market);

                    Logger.LogMessage("Calculated: LowerLeft: Lat: {0} Lng {1} UpperRight: Lat: {2} Lng {3}",
                        lowerLeftForMarket.Latitude, lowerLeftForMarket.Longitude, upperRightForMarket.Latitude, upperRightForMarket.Longitude);
                }
                else
                {
                    Logger.LogMessage("No calculated region for this market since there's no company marked as IsInNetwork");
                }

                foreach (var taxihailNetworkSettings in grouping.OrderBy(x => x.Id))
                {
                    var lowerLeft = new MapCoordinate
                    {
                        Latitude = Math.Min(taxihailNetworkSettings.Region.CoordinateStart.Latitude, taxihailNetworkSettings.Region.CoordinateEnd.Latitude),
                        Longitude = Math.Min(taxihailNetworkSettings.Region.CoordinateStart.Longitude, taxihailNetworkSettings.Region.CoordinateEnd.Longitude)
                    };
                    var upperRight = new MapCoordinate
                    {
                        Latitude = Math.Max(taxihailNetworkSettings.Region.CoordinateStart.Latitude, taxihailNetworkSettings.Region.CoordinateEnd.Latitude),
                        Longitude = Math.Max(taxihailNetworkSettings.Region.CoordinateStart.Longitude, taxihailNetworkSettings.Region.CoordinateEnd.Longitude)
                    };

                    Logger.LogMessage("    Company: {0} (LowerLeft: Lat: {1} Lng {2} UpperRight: Lat: {3} Lng {4}){5}", taxihailNetworkSettings.Id,
                        lowerLeft.Latitude, lowerLeft.Longitude, upperRight.Latitude, upperRight.Longitude, taxihailNetworkSettings.IsInNetwork ? " [IsInNetwork]" : string.Empty);

                    taxihailNetworkSettings.Region = null;
                }

                thNetworkSettingsRepo.Update(grouping);
            }
        }

        private static void MigrateNetworkVehiclesToMarketRepresentation()
        {
            var networkVehicles = new MongoRepository<NetworkVehicle>();
            if (!networkVehicles.Any())
            {
                // Migration already done
                return;
            }

            var allMarketsDefinedInNetworkSettings = new MongoRepository<TaxiHailNetworkSettings>().Select(x => x.Market).Distinct();
            var allMarketsDefinedInNetworkVehicles = networkVehicles.Select(x => x.Market).Distinct();
            var allMarketsDefined = new List<string>();
            allMarketsDefined.AddRange(allMarketsDefinedInNetworkSettings);
            allMarketsDefined.AddRange(allMarketsDefinedInNetworkVehicles);
            allMarketsDefined = allMarketsDefined.Distinct().ToList();

            var marketRepo = new MongoRepository<Market>();

            foreach (var market in allMarketsDefined)
            {
                var vehiclesForThisMarket = networkVehicles
                    .Where(x => x.Market == market)
                    .Select(x => new Vehicle
                    {
                        Id = x.Id,
                        Name = x.Name,
                        LogoName = x.LogoName,
                        MaxNumberPassengers = x.MaxNumberPassengers,
                        NetworkVehicleId = x.NetworkVehicleId
                    })
                    .ToList();

                var newMarket = new Market
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = market,
                    Vehicles = vehiclesForThisMarket
                };

                marketRepo.Add(newMarket);
            }

            networkVehicles.DeleteAll();
            networkVehicles.Collection.Drop();
        }

        private static void EnsureMenuColorIsSet()
        {
            var companies = new MongoRepository<Company>();

            foreach (var c in companies)
            {
                if ( string.IsNullOrEmpty( c.Style.MenuColor  ))
                {
                    //c.Style.MenuColor 
                    c.Style.MenuColor = "#f2f2f2";
                    companies.Update(c);
                }
            }
        }

        private static void EnsureDefaultDevicesAreInit()
        {
            var devices = new MongoRepository<IosDevice>();
            if (!devices.Any(d => d.Id == "092bb6b72e9b0067c86260d47b6eda05f7d43429"))
            {
                devices.Add(new IosDevice { Id = "092bb6b72e9b0067c86260d47b6eda05f7d43429", Name = "MK Support" });
            }

            if (!devices.Any(d => d.Id == "0993f067aa9743a27cf42bce41b00e8ee7c64f97"))
            {
                devices.Add(new IosDevice { Id = "0993f067aa9743a27cf42bce41b00e8ee7c64f97", Name = "MK Support 2 - Denis" });
            }

            if (!devices.Any(d => d.Id == "b6dc4dc7c54b9930f634b22cc1bdd8f846651738"))
            {
                devices.Add(new IosDevice {Id = "b6dc4dc7c54b9930f634b22cc1bdd8f846651738", Name = "MK Chris P"});
            }

            if (!devices.Any(d => d.Id == "2e00e12d970a3be9423369484fdd18f01235c400"))
            {
                devices.Add(new IosDevice { Id = "2e00e12d970a3be9423369484fdd18f01235c400", Name = "apcurium Alex P" });
            }

        }

        private void EnsureDefaultSettingsAreInit()
        {
            var defaultsClient = new Dictionary<string, string>
            {                
                {"CanChangeServiceUrl","false"}, {"ErrorLog",""}, {"ErrorLogEnabled","false"}, {"FacebookAppId","134284363380764"}, {"FacebookEnabled","false"},
                {"ServiceUrlTest","http://services.taxihail.com/taxihaildemo/api/"}, {"TwitterAccessTokenUrl","https://api.twitter.com/oauth/access_token"},
                {"TwitterAuthorizeUrl","https://api.twitter.com/oauth/authorize"}, {"TwitterCallback","http://www.taxihail.com/oauth"},
                {"TwitterConsumerKey","3nNkJ5EcI7yyi56ifLSAA"}, {"TwitterConsumerSecret","Th6nCDTgPiI3JPwHxgm8fQheMaLczUeHHG5liHGZRqs"},
                {"TwitterEnabled","false"},{"TwitterRequestTokenUrl","https://api.twitter.com/oauth/request_token"}, {"TaxiHail.Version", "2.0"}, {"Client.CreditCardIsMandatory", "false"} 
            };

            var settings = new MongoRepository<DefaultCompanySetting>();

            foreach (var defaultSetting in defaultsClient)
            {
                var existing = settings.SingleOrDefault(s => s.Id == defaultSetting.Key);

                if (existing == null)
                {
                    settings.Add(new DefaultCompanySetting {Id = defaultSetting.Key, Value = defaultSetting.Value, IsClient = true});
                }
                if ( (existing != null) && !existing.IsClient )
                {
                    existing.IsClient = true;
                    settings.Update(existing);
                }
                
                if ((existing != null) && (existing.Value != defaultSetting.Value))
                {
                    existing.Value = defaultSetting.Value;
                    settings.Update(existing);
                }

            }

            string path = @"~/assets/DefaultSettings/defaults.json";
            if (!File.Exists(path))
            {
                var json = File.ReadAllText(HostingEnvironment.MapPath(path));
                var serverSettings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                foreach (KeyValuePair<string, string> defaultSetting in serverSettings)
                {
                    var existing = settings.SingleOrDefault(s => s.Id == defaultSetting.Key);
                    if (existing == null)
                    {
                        settings.Add(new DefaultCompanySetting { Id = defaultSetting.Key, Value = defaultSetting.Value, IsClient = false });
                    }                    
                }
            }

        }


//        private static void EnsureEnvironnementsAreInit()
//        {
//            var environments = new MongoRepository<Environment>();
//            if (!environments.Any(e => e.Name == "Staging"))
//            {
//                environments.Add(new Environment
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    Name = "Staging",
//                    Url = "http://staging.taxihail.com",
//                    Role = EnvironmentRole.DeployServer,
//                    SqlServerInstance = "MSSQL10_50.MSSQLSERVER",
//                    WebSitesFolder = @"c:\Data\TaxiHail",
//                    IsProduction = false,
//                });
//            }


//            if (!environments.Any(e => e.Name == "Production"))
//            {
//                environments.Add(new Environment
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    Name = "Production",
//                    Url = "http://services.taxihail.com",
//                    Role = EnvironmentRole.DeployServer,
//                    SqlServerInstance = "MSSQL10_50.MSSQLSERVER",
//                    WebSitesFolder = @"d:\Data\TaxiHail",
//                    IsProduction = true,
//                });
//            }

//            //if (!environments.Any(e => e.Name == "MobileBuildAgent"))
//            //{
//            //    environments.Add(new Environment
//            //    {
//            //        Id = Guid.NewGuid().ToString(),
//            //        Name = "MobileBuildAgent",
//            //        Url = "",
//            //        Role = EnvironmentRole.BuildMobile,
//            //        IsProduction = false,
//            //    });
//            //}

//            if (!environments.Any(e => e.Name == "ServerBuildAgent"))
//            {
//                environments.Add(new Environment
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    Name = "ServerBuildAgent",
//                    Url = "http://test.taxihail.biz:8181",
//                    Role = EnvironmentRole.BuildServer,
//                    SqlServerInstance = "MSSQL10_50.MSSQLSERVER",
//                    WebSitesFolder = @"c:\Data\TaxiHail",
//                    IsProduction = false,
//                });
//            }


//#if DEBUG
//            if (!environments.Any(e => e.Name == "Dev"))
//            {
//                environments.Add(new Environment
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    Name = "Dev",
//                    Url = "http://test.taxihail.biz:8181",
//                    Role = EnvironmentRole.DeployServer,
//                    SqlServerInstance = "",
//                    WebSitesFolder = @"c:\Data\TaxiHail"
//                });
//            }
//#endif
//        }

        private void DeleteTempFiles()
        {
            var tempPath = HostingEnvironment.MapPath("~/App_Data/tempzip/");
            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                }
            }
        }
    }
}