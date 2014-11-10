﻿#region

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Api.Services;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Booking.Database;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Booking.ReadModel.Query;
using apcurium.MK.Booking.Security;
using apcurium.MK.Common;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Configuration.Impl;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Enumeration;
using apcurium.MK.Common.Extensions;
using DatabaseInitializer.Services;
using DatabaseInitializer.Sql;
using Infrastructure.Messaging;
using log4net;
using Microsoft.Practices.Unity;
using Microsoft.Web.Administration;
using MK.Common.Configuration;
using Newtonsoft.Json.Linq;
using DeploymentServiceTools;
using ServiceStack.Text;
using RegisterAccount = apcurium.MK.Booking.Commands.RegisterAccount;

#endregion

namespace DatabaseInitializer
{
    public class Program
    {
        private const string LocalDevProjectName = "MKWebDev";

        private static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private static string CurrentVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();                
            }
        }

        public static int Main(string[] args)
        {
            var logger = LogManager.GetLogger("DatabaseInitializer");
            try
            {
                Console.WriteLine("Creating/updating database for version {0}", CurrentVersion);

                Console.WriteLine("Initializing...");
                var param = GetParamsFromArgs(args);

                Console.WriteLine("Working..." );

                UnityContainer container;
                Module module;

                var creatorDb = new DatabaseCreator();
                var isUpdate = creatorDb.DatabaseExists(param.MasterConnectionString , param.CompanyName);
                IDictionary<string, string> appSettings;

                //for dev company, delete old database to prevent keeping too many databases
                if (param.CompanyName == LocalDevProjectName && isUpdate)
                {
#if DEBUG
                    Console.WriteLine("Drop Existing Database? Y or N");
                    var shouldDrop = args.Length > 2 ? args[2] : Console.ReadLine();
                    if ("Y".Equals(shouldDrop, StringComparison.OrdinalIgnoreCase))
                    {
                        creatorDb.DropDatabase(param.MasterConnectionString, param.CompanyName);
                        isUpdate = false;
                    }
#endif
                }

                if (isUpdate)
                {
                    Console.WriteLine("Update");

                    Console.WriteLine("Create Empty Database");
                    var temporaryDatabaseName = param.CompanyName + "_New";
                    var connectionStringNewDatabase = new ConnectionStringSettings("MkWeb", string.Format("Data Source=.;Initial Catalog={0};Integrated Security=True; MultipleActiveResultSets=True", temporaryDatabaseName));
                    creatorDb.CreateDatabase(param.MasterConnectionString, temporaryDatabaseName, param.SqlInstanceName);
                    creatorDb.CreateSchemas(connectionStringNewDatabase);

                    Console.WriteLine("Copy Events to the Empty Database");
                    creatorDb.CopyEventsAndCacheTables(param.MasterConnectionString, param.CompanyName, temporaryDatabaseName);
                    creatorDb.CopyAppStartUpLogTable(param.MasterConnectionString, param.CompanyName, temporaryDatabaseName);
                    creatorDb.FixUnorderedEvents(connectionStringNewDatabase.ConnectionString);

                    container = new UnityContainer();
                    module = new Module();
                    module.Init(container, connectionStringNewDatabase);

                    Console.WriteLine("Creating index...");
                    creatorDb.CreateIndexes(param.MasterConnectionString, temporaryDatabaseName);

                    Console.WriteLine("Migrating events...");
                    var migrator = container.Resolve<IEventsMigrator>();
                    migrator.Do();

                    Console.WriteLine("Replaying events...");
                    var replayService = container.Resolve<IEventsPlayBackService>();
                    replayService.ReplayAllEvents();
                    Console.WriteLine("Done playing events...");

                    Console.WriteLine("Stop App Pool to finish Database Migration...");
                    var iisManager = new ServerManager();
                    var appPool = iisManager.ApplicationPools.FirstOrDefault(x => x.Name == param.AppPoolName);

                    if (appPool != null
                        && appPool.State == ObjectState.Started)
                    {
                        appPool.Stop();
                    }

                    Console.WriteLine("Copy Events Raised Since the Copy...");
                    var lastEventCopyDateTime = creatorDb.CopyEventsAndCacheTables(param.MasterConnectionString, param.CompanyName, temporaryDatabaseName);
                    replayService.ReplayAllEvents(lastEventCopyDateTime);
                    creatorDb.CopyAppStartUpLogTable(param.MasterConnectionString, param.CompanyName, temporaryDatabaseName);

                    Console.WriteLine("Rename Previous Database...");
                    var mirrored = creatorDb.IsMirroringSet(param.MasterConnectionString, param.CompanyName);
                    if (mirrored)
                    {
                        Console.WriteLine("Turning off database mirroring...");
                        creatorDb.TurnOffMirroring(param.MasterConnectionString, param.CompanyName);
                        Console.WriteLine("Database mirroring turned off.");
                    }   
                    var oldDatabase = creatorDb.RenameDatabase(param.MasterConnectionString, param.CompanyName);

                    Console.WriteLine("Rename New Database to use Company Name...");
                    creatorDb.RenameDatabase(param.MasterConnectionString, temporaryDatabaseName, param.CompanyName);

                    Console.WriteLine("Restart App Pool...");
                    if (appPool != null
                       && appPool.State == ObjectState.Stopped)
                    {
                        appPool.Start();
                    }

                    if (!string.IsNullOrEmpty(param.BackupFolder))
                    {
                        Console.WriteLine("Backup of old database...");
                        var backupFolder = Path.Combine(param.BackupFolder, param.CompanyName + DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss"));
                        Directory.CreateDirectory(backupFolder);
                        creatorDb.BackupDatabase(param.MasterConnectionString, backupFolder, oldDatabase);

                        Console.WriteLine("Dropping of old database...");
                        creatorDb.DropDatabase(param.MasterConnectionString, oldDatabase);
                    }
                }
                else
                {
                    creatorDb.CreateDatabase(param.MasterConnectionString, param.CompanyName, param.SqlInstanceName);
                    creatorDb.CreateSchemas(new ConnectionStringSettings("MkWeb", param.MkWebConnectionString));
                    creatorDb.CreateIndexes(param.MasterConnectionString, param.CompanyName);

                    Console.WriteLine("Add user for IIS...");
                    if (param.MkWebConnectionString.ToLower().Contains("integrated security=true"))
                    {
                        creatorDb.AddUserAndRighst(param.MasterConnectionString, param.MkWebConnectionString,
                            "IIS APPPOOL\\" + param.AppPoolName, param.CompanyName);
                    }
                }

                var connectionString = new ConnectionStringSettings("MkWeb", param.MkWebConnectionString);
                container = new UnityContainer();
                module = new Module();
                module.Init(container, connectionString); 

                var serverSettings = container.Resolve<IServerSettings>();
                var commandBus = container.Resolve<ICommandBus>();


                Console.WriteLine("Checking Company Created...");
                CheckCompanyCreated(container, commandBus);

                if (!isUpdate)
                {
                    appSettings = GetCompanySettings(param.CompanyName);

                    //Save settings so that next calls to referenceDataService has the IBS Url
                    AddOrUpdateAppSettings(commandBus, appSettings);

                    FetchingIbsDefaults(container, commandBus);

                    CreateDefaultAccounts(container, commandBus);
                }
                else
                {
                    appSettings = serverSettings.GetSettings();
                }

                Console.WriteLine("Checking Rules...");
                CheckandMigrateDefaultRules(connectionString, commandBus, appSettings);
                Console.WriteLine("Checking Default Account Settings ...");
                EnsureDefaultAccountsHasCorrectSettings(connectionString, commandBus);

                Console.WriteLine("Checking Default Tariff ...");
                CreateDefaultTariff(connectionString.ConnectionString, serverSettings, commandBus);
                
                Console.WriteLine("Checking Ratings ...");
                var ratingTypes = new RatingTypeDao(() => new BookingDbContext(connectionString.ConnectionString)).GetAll();
                if (!ratingTypes.Any())
                {
                    AddDefaultRatings(commandBus);
                }
                else
                {
                    UpdateRatings(commandBus, ratingTypes);
                }

                Console.WriteLine("Checking Vehicles Types ...");
                var vehicleTypes = new VehicleTypeDao(() => new BookingDbContext(connectionString.ConnectionString));
                if (!vehicleTypes.GetAll().Any())
                {
                    appSettings["VehicleTypeSelectionEnabled"] = "false";
                    AddOrUpdateAppSettings(commandBus, appSettings);
                    CreateDefaultVehicleTypes(container, commandBus);
                }

                Console.WriteLine("Migration of Notification Settings ...");
                var configDao = new ConfigurationDao(() => new ConfigurationDbContext(connectionString.ConnectionString));
                if (configDao.GetNotificationSettings() == null)
                {
                    commandBus.Send(new AddOrUpdateNotificationSettings
                    {
                        CompanyId = AppConstants.CompanyId,
                        NotificationSettings = new NotificationSettings
                        {
                            Enabled = true,
                            BookingConfirmationEmail = true,
                            ConfirmPairingPush = true,
                            DriverAssignedPush = true,
                            NearbyTaxiPush = true,
                            PaymentConfirmationPush = true,
                            ReceiptEmail = true,
                            VehicleAtPickupPush = true
                        }
                    });
                }
                SetupMirroring(param);

                Console.WriteLine("Database Creation/Migration for version {0} finished", CurrentVersion);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "-" + e);
                logger.Fatal(e.ToString());
                logger.Fatal(e.Message, e);
                return 1;
            }
            return 0;
// ReSharper restore LocalizableElement
        }

        public static void SetupMirroring(DatabaseInitializerParams param)
        {
            if (!string.IsNullOrEmpty(param.MirroringSharedFolder))
            {
                Console.WriteLine("Add Mirroring on the new Database ...");
                var creatorDb = new DatabaseCreator();
                Console.WriteLine("MirroringSharedFolder :" + param.MirroringSharedFolder);
                Console.WriteLine("Setting up mirroring...");
                var backupFolder = Path.Combine(param.MirroringSharedFolder, param.CompanyName + DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss"));
                Directory.CreateDirectory(backupFolder);

                creatorDb.InitMirroring(param.MasterConnectionString, param.CompanyName);
                Console.WriteLine("Backup for mirroring...");
                creatorDb.BackupDatabase(param.MasterConnectionString, backupFolder, param.CompanyName);

                var mirrorDbExist = creatorDb.DatabaseExists(param.MirrorMasterConnectionString, param.CompanyName);
                if (mirrorDbExist)
                {
                    Console.WriteLine("Deleting existing mirroring db...");
                    creatorDb.RestoreToDeleteDatabase(param.MirrorMasterConnectionString, backupFolder, param.CompanyName);
                    creatorDb.DropDatabase(param.MirrorMasterConnectionString, param.CompanyName);
                }

                Console.WriteLine("Restoring mirroring backup...");
                creatorDb.RestoreDatabase(param.MirrorMasterConnectionString, backupFolder, param.CompanyName);

                creatorDb.SetMirroringPartner(param.MirrorMasterConnectionString, param.CompanyName, param.MirroringMirrorPartner);
                creatorDb.CompleteMirroring(param.MasterConnectionString, param.CompanyName, param.MirroringPrincipalPartner, param.MirroringWitness);
            }
        }

        private static void CheckCompanyCreated(UnityContainer container, ICommandBus commandBus)
        {
            var companyIsCreated = container.Resolve<IEventsPlayBackService>().CountEvent("Company") > 0;

            if (!companyIsCreated)
            {
                // Create Default company
                commandBus.Send(new CreateCompany
                {
                    CompanyId = AppConstants.CompanyId,
                    Id = Guid.NewGuid()
                });
            }
        }

        private static void UpdateRatings(ICommandBus commandBus, IEnumerable<RatingTypeDetail[]> ratingTypes)
        {
            var supportedLanguages = Enum.GetNames(typeof(SupportedLanguages));

            foreach (var ratingType in ratingTypes)
            {
                var ratingTypeLanguages = ratingType.Select(t => t.Language);
                var missingRatingTypeLanguages = supportedLanguages.Except(ratingTypeLanguages).ToArray();

                if (missingRatingTypeLanguages.Any())
                {
                    // Every rating share the same ID for one language
                    var ratingId = ratingType.First().Id;

                    // Take english name by default. If none, take first that we find.
                    var ratingName =
                            ratingType.FirstOrDefault(r => r.Language == SupportedLanguages.en.ToString())
                                      .SelectOrDefault(r => r.Name);

                    if (ratingName.IsNullOrEmpty())
                    {
                        ratingName = ratingType.First().Name;
                    }

                    // Add missing language
                    foreach (var missingRatingTypeLanguage in missingRatingTypeLanguages)
                    {
                        commandBus.Send(new AddRatingType
                        {
                            CompanyId = AppConstants.CompanyId,
                            Name = ratingName,
                            RatingTypeId = ratingId,
                            Language = missingRatingTypeLanguage
                        });
                    }
                }
            }
        }

        private static void AddDefaultRatings(ICommandBus commandBus)
        {
            var knowledgeId = Guid.NewGuid();
            var politenessId = Guid.NewGuid();
            var safetyId = Guid.NewGuid();
            var supportedLanguages = Enum.GetNames(typeof(SupportedLanguages));

            foreach (var supportedLanguage in supportedLanguages)
            {
                commandBus.Send(new AddRatingType
                {
                    CompanyId = AppConstants.CompanyId,
                    Name = "Knowledgable driver",
                    RatingTypeId = knowledgeId,
                    Language = supportedLanguage
                });
            }

            foreach (var supportedLanguage in supportedLanguages)
            {
                commandBus.Send(new AddRatingType
                {
                    CompanyId = AppConstants.CompanyId,
                    Name = "Politeness",
                    RatingTypeId = politenessId,
                    Language = supportedLanguage
                });
            }

            foreach (var supportedLanguage in supportedLanguages)
            {
                commandBus.Send(new AddRatingType
                {
                    CompanyId = AppConstants.CompanyId,
                    Name = "Safety",
                    RatingTypeId = safetyId,
                    Language = supportedLanguage
                });
            }
        }

        private static void CreateDefaultAccounts(UnityContainer container, ICommandBus commandBus)
        {
            Console.WriteLine("Register normal account...");
            //Register normal account
            var registerAccountCommand = new RegisterAccount
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Email = "john@taxihail.com",
                Name = "John Doe",
                Phone = "6132875020",
                Password = "password"
            };

            var confirmationToken = Guid.NewGuid();
            registerAccountCommand.ConfimationToken = confirmationToken.ToString();

            commandBus.Send(registerAccountCommand);

            commandBus.Send(new ConfirmAccount
            {
                AccountId = registerAccountCommand.AccountId,
                ConfimationToken = registerAccountCommand.ConfimationToken
            });

            //Register admin account
            Console.WriteLine("Register admin account...");
            var registerAdminAccountCommand = new RegisterAccount
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Email = "taxihail@apcurium.com",
                Name = "Administrator",
                Phone = "6132875020",
                Password = "1l1k3B4n4n@", //Todo Make the admin portal customizable
                IsAdmin = true
            };

            var confirmationAdminToken = Guid.NewGuid();
            registerAdminAccountCommand.ConfimationToken = confirmationAdminToken.ToString();

            commandBus.Send(registerAdminAccountCommand);
            commandBus.Send(new AddRoleToUserAccount
            {
                AccountId = registerAdminAccountCommand.AccountId,
                RoleName = RoleName.SuperAdmin,
            });

            commandBus.Send(new ConfirmAccount
            {
                AccountId = registerAdminAccountCommand.AccountId,
                ConfimationToken = registerAdminAccountCommand.ConfimationToken
            });
        }

        private static DatabaseInitializerParams GetParamsFromArgs(string[] args)
        {
            var result = new DatabaseInitializerParams();

            Console.WriteLine("args : " + args.JoinBy(" "));
            if (args.Any() && !string.IsNullOrWhiteSpace(args[0]) && args[0].Trim().StartsWith("f:"))
            {
                var paramFile =  Path.Combine( AssemblyDirectory , args[0].Trim().Substring(2, args[0].Trim().Length - 2));
                Console.WriteLine("Reading param file : " + paramFile );
                if ( !File.Exists(paramFile ))
                {
                    throw new ApplicationException("Parameteres file cannot be found");
                }
                var paramFileContent = File.ReadAllText(paramFile);

                result = JsonSerializer.DeserializeFromString<DatabaseInitializerParams>(paramFileContent); 
            }
            else if (args.Length > 0)
            {
                result.CompanyName = args[0];
            }

            result.CompanyName = string.IsNullOrWhiteSpace(result.CompanyName) ? LocalDevProjectName : result.CompanyName;

            //Sql instance name
            if (string.IsNullOrWhiteSpace(result.MkWebConnectionString) && (args.Length > 1))
            {
                result.SqlInstanceName = args[1];
            }
            else if (string.IsNullOrWhiteSpace(result.SqlInstanceName))
            {
                var sqlInstanceName = "MSSQL11.MSSQLSERVER";
                Console.WriteLine("Sql Instance name ? Default is MSSQL11.MSSQLSERVER , 1- MSSQL10_50.MSSQLSERVER, 2- MSSQL12.MSSQLSERVER");

                var userSqlInstance = Console.ReadLine();

                sqlInstanceName = string.IsNullOrEmpty(userSqlInstance)
                    ? sqlInstanceName
                    : userSqlInstance == "1"
                    ? "MSSQL10_50.MSSQLSERVER"
                    : userSqlInstance == "2"
                    ? "MSSQL12.MSSQLSERVER"
                    : sqlInstanceName;

                result.SqlInstanceName = sqlInstanceName;
            }
            
            //Company connection string
            if (string.IsNullOrWhiteSpace(result.MkWebConnectionString) && (args.Length > 3))
            {
                result.MkWebConnectionString = string.Format(args[3], result.CompanyName);
            }
            else if (string.IsNullOrWhiteSpace(result.MkWebConnectionString))
            {
                result.MkWebConnectionString = string.Format("Data Source=.;Initial Catalog={0};Integrated Security=True; MultipleActiveResultSets=True", result.CompanyName);
            }

            //Master connection string           
            if (string.IsNullOrWhiteSpace(result.MasterConnectionString) && (args.Length > 4))
            {
                result.MasterConnectionString = string.Format(args[4], result.CompanyName);
            }
            else if (string.IsNullOrWhiteSpace(result.MasterConnectionString))
            {
                result.MasterConnectionString = result.MkWebConnectionString.Replace(result.CompanyName, "master");
            }

            if (string.IsNullOrWhiteSpace(result.AppPoolName))
            {
                result.AppPoolName = "DefaultAppPool";
            }

            Console.WriteLine("Running database initializer using the following settings : ");

            var j = result.ToJson();
            Console.WriteLine(j);

            return result;
        }

        private static void FetchingIbsDefaults(UnityContainer container, ICommandBus commandBus)
        {
            var appSettings = new Dictionary<string, string>(); 
            Console.WriteLine("Calling ibs...");
            //Get default settings from IBS
            var ibsServiceProvider = container.Resolve<IIBSServiceProvider>();

            var defaultCompany = ibsServiceProvider.StaticData().GetCompaniesList()
                .FirstOrDefault(x => x.IsDefault.HasValue && x.IsDefault.Value)
                                 ?? ibsServiceProvider.StaticData().GetCompaniesList().FirstOrDefault();

            if (defaultCompany != null)
            {
                appSettings["DefaultBookingSettings.ProviderId"] = defaultCompany.Id.ToString();

                var defaultvehicule =
                    ibsServiceProvider.StaticData().GetVehiclesList(defaultCompany)
                        .FirstOrDefault(x => x.IsDefault.HasValue && x.IsDefault.Value) ??
                    ibsServiceProvider.StaticData().GetVehiclesList(defaultCompany).First();
                appSettings["DefaultBookingSettings.VehicleTypeId"] = defaultvehicule.Id.ToString();

                appSettings["DefaultBookingSettings.ChargeTypeId"] = ChargeTypes.PaymentInCar.Id.ToString();
            }

            //Save settings so that registerAccountCommand succeed
            AddOrUpdateAppSettings(commandBus, appSettings);         
        }

        private static void EnsureDefaultAccountsHasCorrectSettings(ConnectionStringSettings connectionString, ICommandBus commandBus)
        {
            var accounts = new AccountDao(() => new BookingDbContext(connectionString.ConnectionString));
            var admin = accounts.FindByEmail("taxihail@apcurium.com");

            if (admin != null
                && (!admin.IsAdmin || !admin.IsConfirmed))
            {
                commandBus.Send(new AddRoleToUserAccount
                {
                    AccountId = admin.Id,
                    RoleName = RoleName.SuperAdmin,
                });


                commandBus.Send(new ConfirmAccount
                {
                    AccountId = admin.Id,
                    ConfimationToken = admin.ConfirmationToken
                });
            }

            var john = accounts.FindByEmail("john@taxihail.com");

            if (john != null)
            {
                var updateAccountCommand = new UpdateBookingSettings
                {
                    AccountId = john.Id,
                    Phone = "6132875020",
                    Name = "John Doe",
                    NumberOfTaxi = john.Settings.NumberOfTaxi,
                    ChargeTypeId = john.Settings.ChargeTypeId,
                    DefaultCreditCard = john.DefaultCreditCard,
                    DefaultTipPercent = john.DefaultTipPercent
                };
                commandBus.Send(updateAccountCommand);
            }
        }

        private static void CheckandMigrateDefaultRules(ConnectionStringSettings connectionString, ICommandBus commandBus, IDictionary<string, string> appSettings)
        {
            var rules = new RuleDao(() => new BookingDbContext(connectionString.ConnectionString));
            if (
                rules.GetAll()
                    .None(r => (r.Category == (int) RuleCategory.WarningRule) && (r.Type == (int) RuleType.Default)))
            {
                // Default rate does not exist for this company 
                commandBus.Send(new CreateRule
                {
                    Type = RuleType.Default,
                    Category = RuleCategory.WarningRule,
                    CompanyId = AppConstants.CompanyId,
                    AppliesToCurrentBooking = true,
                    AppliesToFutureBooking = true,
                    RuleId = Guid.NewGuid(),
                    IsActive = false,
                    Name = "Default",
                    Message = "Due to the current volume of calls, please note that pickup may be delayed.",
                    ZoneList = " "
                });
            }

            if (
                rules.GetAll()
                    .None(r => (r.Category == (int) RuleCategory.DisableRule) && (r.Type == (int) RuleType.Default)))
            {
                // Default rate does not exist for this company 
                commandBus.Send(new CreateRule
                {
                    Type = RuleType.Default,
                    Category = RuleCategory.DisableRule,
                    CompanyId = AppConstants.CompanyId,
                    AppliesToCurrentBooking = true,
                    AppliesToFutureBooking = true,
                    RuleId = Guid.NewGuid(),
                    Name = "Default",
                    Message = "Service is temporarily unavailable. Please call dispatch center for service.",
                });
            }

            var priority = rules.GetAll().Max(x => x.Priority) + 1;

            //validation of the pickup zone
            if (rules.GetAll()
                    .None(r => (r.Category == (int)RuleCategory.DisableRule) 
                                && r.Type == (int)RuleType.Default
                                && r.AppliesToPickup
                                && r.ZoneRequired))
            {
                //Validation of the pickup
                commandBus.Send(new CreateRule
                {
                    Type = RuleType.Default,
                    Category = RuleCategory.DisableRule,
                    CompanyId = AppConstants.CompanyId,
                    AppliesToCurrentBooking = true,
                    AppliesToFutureBooking = true,
                    AppliesToPickup = true,
                    ZoneRequired = true,
                    RuleId = Guid.NewGuid(),
                    Name = "Pickup Zone Required",
                    Message = "The specified Pickup address lies outside the regular service area.",
                    IsActive = true,
                    Priority = priority++
                });
            }

            //check if any pickup exclusion
            if (appSettings.ContainsKey("IBS.PickupZoneToExclude")
                && !string.IsNullOrWhiteSpace(appSettings["IBS.PickupZoneToExclude"])
                && rules.GetAll()
                    .None(r => (r.Category == (int)RuleCategory.DisableRule)
                                && r.Type == (int)RuleType.Default
                                && r.AppliesToPickup
                                && r.ZoneList == appSettings["IBS.PickupZoneToExclude"]))
            {

                commandBus.Send(new CreateRule
                {
                    Type = RuleType.Default,
                    Category = RuleCategory.DisableRule,
                    CompanyId = AppConstants.CompanyId,
                    AppliesToCurrentBooking = true,
                    AppliesToFutureBooking = true,
                    AppliesToPickup = true,
                    ZoneList = appSettings["IBS.PickupZoneToExclude"],
                    RuleId = Guid.NewGuid(),
                    Name = "Pickup Zone Exclusion",
                    Message = "We don't serve this pickup location",
                    IsActive = true,
                    Priority = priority++
                });

                appSettings["IBS.PickupZoneToExclude"] = null;
            }

            //check if dropoff zone is required
            if (appSettings.ContainsKey("IBS.ValidateDestinationZone")
                && !string.IsNullOrWhiteSpace(appSettings["IBS.ValidateDestinationZone"])
                && rules.GetAll()
                    .None(r => (r.Category == (int)RuleCategory.DisableRule)
                                && r.Type == (int)RuleType.Default
                                && r.AppliesToDropoff
                                && r.ZoneRequired))
            {

                commandBus.Send(new CreateRule
                {
                    Type = RuleType.Default,
                    Category = RuleCategory.DisableRule,
                    CompanyId = AppConstants.CompanyId,
                    AppliesToCurrentBooking = true,
                    AppliesToFutureBooking = true,
                    AppliesToDropoff = true,
                    ZoneRequired = true,
                    RuleId = Guid.NewGuid(),
                    Name = "Dropoff Zone Required",
                    Message = "The specified Dropoff address lies outside the regular service area",
                    IsActive = bool.Parse(appSettings["IBS.ValidateDestinationZone"]),
                    Priority = priority++
                });
                appSettings["IBS.ValidateDestinationZone"] = null;
            }

            //check if any dropoff exclusion
            if (appSettings.ContainsKey("IBS.DestinationZoneToExclude")
                && !string.IsNullOrWhiteSpace(appSettings["IBS.DestinationZoneToExclude"])
                && rules.GetAll()
                    .None(r => (r.Category == (int)RuleCategory.DisableRule)
                                && r.Type == (int)RuleType.Default
                                && r.AppliesToDropoff
                                && r.ZoneList == appSettings["IBS.DestinationZoneToExclude"]))
            {

                commandBus.Send(new CreateRule
                {
                    Type = RuleType.Default,
                    Category = RuleCategory.DisableRule,
                    CompanyId = AppConstants.CompanyId,
                    AppliesToCurrentBooking = true,
                    AppliesToFutureBooking = true,
                    AppliesToDropoff = true,
                    ZoneList = appSettings["IBS.DestinationZoneToExclude"],
                    RuleId = Guid.NewGuid(),
                    Name = "Dropoff Zone Exclusion",
                    Message = "We don't serve this dropoff location",
                    IsActive = true,
                    Priority = priority++
                });
                appSettings["IBS.DestinationZoneToExclude"] = null;
            }

            AddOrUpdateAppSettings(commandBus, appSettings);
        }

        private static Dictionary<string, string> GetCompanySettings(string companyName)
        {            
            // Create settings
            var appSettings = new Dictionary<string, string>();

            Console.WriteLine("Loading company settings...");

            var jsonSettings = File.ReadAllText(Path.Combine(AssemblyDirectory, "Settings\\", companyName + ".json"));
            var objectSettings = JObject.Parse(jsonSettings);

            foreach (var token in objectSettings)
            {
                appSettings[token.Key] = token.Value.ToString();
            }
            
            return appSettings;
        }

        private static void AddOrUpdateAppSettings(ICommandBus commandBus, IDictionary<string, string> appSettings)
        {
            commandBus.Send(new AddOrUpdateAppSettings
            {
                AppSettings = appSettings,
                CompanyId = AppConstants.CompanyId
            });
        }

        private static void CreateDefaultTariff(string connectionString, IServerSettings serverSettings, ICommandBus commandBus)
        {
            var tariffs = new TariffDao(() => new BookingDbContext(connectionString));
            if (tariffs.GetAll().All(x => x.Type != (int) TariffType.Default))
            {
                commandBus.Send(new CreateTariff
                {
                    Type = TariffType.Default,
                    KilometricRate = serverSettings.ServerData.Direction.RatePerKm,
                    FlatRate = serverSettings.ServerData.Direction.FlateRate,
                    MarginOfError = 20,
                    CompanyId = AppConstants.CompanyId,
                    TariffId = Guid.NewGuid(),
                });
            }
        }

        private static void CreateDefaultVehicleTypes(UnityContainer container, ICommandBus commandBus)
        {
            var referenceDataService = container.Resolve<ReferenceDataService>();
            var referenceData = (ReferenceData) referenceDataService.Get(new ReferenceDataRequest());

            foreach (var vehicle in referenceData.VehiclesList)
            {
                commandBus.Send(new AddUpdateVehicleType
                {
                    VehicleTypeId = Guid.NewGuid(),
                    Name = string.Format("{0}", vehicle.Display),
                    LogoName = "taxi",
                    ReferenceDataVehicleId = vehicle.Id ?? -1,
                    CompanyId = AppConstants.CompanyId
                });
            }
        }
    }
}