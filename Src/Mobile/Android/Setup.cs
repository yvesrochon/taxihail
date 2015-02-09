using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using apcurium.MK.Booking.Mobile.AppServices.Social;
using apcurium.MK.Booking.Mobile.AppServices.Social.OAuth;
using apcurium.MK.Booking.Mobile.Client.Activities.Book;
using apcurium.MK.Booking.Mobile.Client.Binding;
using apcurium.MK.Booking.Mobile.Client.Cache;
using apcurium.MK.Booking.Mobile.Client.Controls;
using apcurium.MK.Booking.Mobile.Client.Converters;
using apcurium.MK.Booking.Mobile.Client.Diagnostic;
using apcurium.MK.Booking.Mobile.Client.Localization;
using apcurium.MK.Booking.Mobile.Client.PlatformIntegration;
using apcurium.MK.Booking.Mobile.Client.Services.Social;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Booking.Mobile.IoC;
using apcurium.MK.Booking.Mobile.Settings;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Diagnostic;
using Cirrious.CrossCore.Droid.Platform;
using Cirrious.MvvmCross.Binding.Bindings.Target.Construction;
using Cirrious.MvvmCross.Dialog.Droid;
using Cirrious.MvvmCross.ViewModels;
using TinyIoC;
using apcurium.MK.Booking.MapDataProvider;
using apcurium.MK.Booking.MapDataProvider.Google;
using Cirrious.CrossCore.Droid;
using apcurium.MK.Common.Entity;
using apcurium.MK.Booking.MapDataProvider.TomTom;
using apcurium.MK.Booking.Mobile.Client.Helpers;
using MK.Booking.MapDataProvider.Foursquare;

namespace apcurium.MK.Booking.Mobile.Client
{
	public class Setup : MvxAndroidDialogSetup
    {
        readonly TinyIoCContainer _container;

		public Setup(Context applicationContext) : base(applicationContext)
        {
            _container = TinyIoCContainer.Current;
        }

		protected override IMvxApplication CreateApp()
		{
			return new TaxiHailApp();
		}

		protected override List<Type> ValueConverterHolders
		{
			get
			{
				return new List<Type> { typeof(AppConverters) };
			}
		}

		protected override void InitializeLastChance()
        {
			base.InitializeLastChance();

            _container.Register<IPackageInfo>(new PackageInfo(ApplicationContext));
            _container.Register<ILogger, LoggerImpl>();
            _container.Register<IMessageService, MessageService>();
            _container.Register<IAnalyticsService>((c, x) => new GoogleAnalyticsService(Application.Context, c.Resolve<IPackageInfo>(), c.Resolve<IAppSettings>(), c.Resolve<ILogger>()));

            _container.Register<ILocationService, LocationService>();

			_container.Register<ILocalization>(new Localize(ApplicationContext,_container.Resolve<ILogger>()));
            _container.Register<ICacheService>(new CacheService());
            _container.Register<ICacheService>(new CacheService("MK.Booking.Application.Cache"), "UserAppCache");
            _container.Register<IPhoneService>(new PhoneService(ApplicationContext));
            _container.Register<IPushNotificationService>((c, p) => new PushNotificationService(ApplicationContext, c.Resolve<IAppSettings>()));

            _container.Register<IAppSettings>(new AppSettingsService (_container.Resolve<ICacheService> (), _container.Resolve<ILogger> ()));

		    _container.Register<IPayPalConfigurationService, PayPalConfigurationService>();

            ConfigureInsights ();

            _container.Register<IGeocoder>((c,p) => new GoogleApiClient(c.Resolve<IAppSettings>(), c.Resolve<ILogger>(), new AndroidGeocoder(c.Resolve<IAppSettings>(), c.Resolve<ILogger>(), c.Resolve<IMvxAndroidGlobals>())));
			_container.Register<IPlaceDataProvider, FoursquareProvider>();
			
            _container.Register<IDirectionDataProvider> ((c, p) =>
            {
                switch (c.Resolve<IAppSettings>().Data.DirectionDataProvider)
                {
	                case MapProvider.TomTom:
	                    return new TomTomProvider(c.Resolve<IAppSettings>(), c.Resolve<ILogger>());
	                case MapProvider.Google:
	                default:
	                    return new GoogleApiClient(c.Resolve<IAppSettings>(), c.Resolve<ILogger>(), new AndroidGeocoder(c.Resolve<IAppSettings>(), c.Resolve<ILogger>(), c.Resolve<IMvxAndroidGlobals>()));
                }
            });

			InitializeSocialNetwork();
        }

		protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry)
        {
            base.FillTargetFactories(registry);
            registry.RegisterFactory(new MvxCustomBindingFactory<EditTextRightSpinner>("SelectedItem", spinner => new EditTextRightSpinnerSelectedItemBinding(spinner)));
            registry.RegisterFactory(new MvxCustomBindingFactory<EditTextSpinner>("SelectedItem", spinner => new EditTextSpinnerSelectedItemBinding(spinner)));
			registry.RegisterFactory(new MvxCustomBindingFactory<EditTextLeftImage>("CreditCardNumber", editTextLeftImage => new EditTextCreditCardNumberBinding(editTextLeftImage)));
			registry.RegisterFactory(new MvxCustomBindingFactory<ListViewCell>("IsBottom", cell => new CellItemBinding(cell, CellItemBindingProperty.IsBottom)));
			registry.RegisterFactory(new MvxCustomBindingFactory<ListViewCell>("IsTop", cell => new CellItemBinding(cell, CellItemBindingProperty.IsTop)));
            CustomBindingsLoader.Load(registry);
        }

		protected override Cirrious.MvvmCross.Droid.Views.MvxAndroidLifetimeMonitor CreateLifetimeMonitor ()
		{
			return new TaxiHailAndroidLifetimeMonitor ();

		}

		private void InitializeSocialNetwork()
		{
			_container.Register<IFacebookService,FacebookService> ();

            _container.Register<ITwitterService>((c,p) => 
            {
                var settings = c.Resolve<IAppSettings>();
                var oauthConfig = new OAuthConfig
                {
                    ConsumerKey = settings.Data.TwitterConsumerKey,
                    Callback = settings.Data.TwitterCallback,
                    ConsumerSecret = settings.Data.TwitterConsumerSecret,
                    RequestTokenUrl = settings.Data.TwitterRequestTokenUrl,
                    AccessTokenUrl = settings.Data.TwitterAccessTokenUrl,
                    AuthorizeUrl = settings.Data.TwitterAuthorizeUrl
                };
                return new TwitterServiceMonoDroid( oauthConfig, c.Resolve<IMvxAndroidCurrentTopActivity>());
            });
		}

		protected override Cirrious.CrossCore.IoC.IMvxIoCProvider CreateIocProvider()
		{
			return new TinyIoCProvider(TinyIoCContainer.Current);
		}

        protected override Cirrious.MvvmCross.Droid.Views.IMvxAndroidViewPresenter CreateViewPresenter()
        {
            return new PhonePresenter();
        }

        private void ConfigureInsights ()
        {
            #if !DEBUG
            
            var settings = TinyIoCContainer.Current.Resolve<IAppSettings>().Data;
            var packageInfo = TinyIoCContainer.Current.Resolve<IPackageInfo>();

            Xamarin.Insights.Initialize(settings.Insights.APIKey, ApplicationContext);
            Xamarin.Insights.DisableCollection = false;
            Xamarin.Insights.DisableDataTransmission = false;
            Xamarin.Insights.DisableExceptionCatching = false;

            // identify with an unknown user in case an exception occurs before the user can log in
            Xamarin.Insights.Identify(settings.Insights.UnknownUserIdentifier, new Dictionary<string, string>
                {
                    { "ApplicationVersion", packageInfo.Version },
                    { "Company", settings.TaxiHail.ApplicationName },
                });
            
            #endif
        }
    }
}