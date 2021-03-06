using System;
using System.Collections.Generic;
using apcurium.MK.Booking.Mobile.Client.Cache;
using apcurium.MK.Booking.Mobile.Client.Diagnostics;
using apcurium.MK.Booking.Mobile.Client.Localization;
using apcurium.MK.Booking.Mobile.IoC;
using apcurium.MK.Common.Configuration;
using Cirrious.MvvmCross.Touch.Platform;
using TinyIoC;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Booking.Mobile.Client.PlatformIntegration;
using apcurium.MK.Booking.Mobile.Client.PlatformIntegration.Social;
using apcurium.MK.Booking.Mobile.Settings;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Booking.Mobile.Client.Converters;
using apcurium.MK.Booking.Mobile.Client.Binding;
using Cirrious.MvvmCross.Binding.Bindings.Target.Construction;
using UIKit;
using apcurium.MK.Booking.Mobile.Client.Controls.Binding;
using apcurium.MK.Booking.Mobile.AppServices.Social;
using apcurium.MK.Booking.Mobile.AppServices.Social.OAuth;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.Dialog.Touch;
using apcurium.MK.Booking.MapDataProvider;
using apcurium.MK.Common.Entity;
using apcurium.MK.Booking.MapDataProvider.TomTom;
using MK.Booking.MapDataProvider.Foursquare;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Common;
using apcurium.MK.Common.Services;
using PCLCrypto;

namespace apcurium.MK.Booking.Mobile.Client
{
	public class Setup : MvxTouchDialogSetup
	{
		public Setup (MvxApplicationDelegate applicationDelegate, UIWindow window)
			: base (applicationDelegate, window)
		{
		}

		protected override IMvxApplication CreateApp ()
		{
			return new TaxiHailApp ();
		}

		protected override List<Type> ValueConverterHolders 
        {
			get { return new List<Type> { typeof(AppConverters) }; }
		}

		protected override void AddPluginsLoaders (Cirrious.CrossCore.Plugins.MvxLoaderPluginRegistry loaders)
		{
			loaders.AddConventionalPlugin<Cirrious.MvvmCross.Plugins.DownloadCache.Touch.Plugin> ();
			loaders.AddConventionalPlugin<Cirrious.MvvmCross.Plugins.File.Touch.Plugin> ();

			base.AddPluginsLoaders (loaders);
		}

		protected override void FillTargetFactories (IMvxTargetBindingFactoryRegistry registry)
		{
			base.FillTargetFactories (registry);
			registry.RegisterFactory (new MvxSimplePropertyInfoTargetBindingFactory (typeof(MvxUITextViewTargetBinding), typeof(UITextView), "Text"));
			CustomBindingsLoader.Load (registry);
		}

		protected override void InitializeLastChance ()
		{
			base.InitializeLastChance ();
         
			var container = TinyIoCContainer.Current;

            container.Register<IAnalyticsService, GoogleAnalyticsService> ();
            container.Register<ICacheService> (new CacheService ());
            container.Register<ICacheService> (new CacheService ("MK.Booking.Application.Cache"), "UserAppCache");
            container.Register<ILocationService> (new LocationService ());
            container.Register<IMessageService, MessageService> ();
            container.Register<ISymmetricKeyAlgorithmProviderFactory>((c, x) => WinRTCrypto.SymmetricKeyAlgorithmProvider);
            container.Register<ICryptographicEngine>((c, x) => WinRTCrypto.CryptographicEngine);
            container.Register<IHashAlgorithmProviderFactory>((c, x) => WinRTCrypto.HashAlgorithmProvider);
            container.Register<IConnectivityService, ConnectivityService> ();
			container.Register<IPackageInfo> (new PackageInfo ());
            container.Register<IIPAddressManager, IPAddressManager>();
			container.Register<ILocalization, Localize> ();
			container.Register<ILogger, LoggerWrapper> ();        
			container.Register<IPhoneService, PhoneService> ();
			container.Register<IPushNotificationService> (new PushNotificationService (container.Resolve<ICacheService> ()));
            container.Register<IAppSettings> (new AppSettingsService (container.Resolve<ICacheService> (), container.Resolve<ILogger> (), container.Resolve<ICryptographyService>()));
            container.Register<IPayPalConfigurationService, PayPalConfigurationService>();
            container.Register<IGeocoder> ((c, p) => new AppleGeocoder ());
            container.Register<IPlaceDataProvider, FoursquareProvider> ();
            container.Register<IDeviceOrientationService, AppleDeviceOrientationService>().AsSingleton();
            container.Register<IDeviceRateApplicationService, AppleDeviceRateApplicationService>();
            container.Register<IQuitApplicationService, QuitApplicationService>();
            container.Register<IDeviceCollectorService, DeviceCollectorService>();

            container.Register<IDirectionDataProvider> ((c, p) =>
            {
                switch (c.Resolve<IAppSettings>().Data.DirectionDataProvider)
                {
                    case MapProvider.TomTom:
                            return new TomTomProvider(c.Resolve<IAppSettings>(), c.Resolve<ILogger>(), c.Resolve<IConnectivityService>());
                    case MapProvider.Google:
                    default:
						return new AppleDirectionProvider( c.Resolve<ILogger>());
                }
            });

            Cirrious.MvvmCross.Plugins.DownloadCache.PluginLoader.Instance.EnsureLoaded ();
            Cirrious.MvvmCross.Plugins.File.PluginLoader.Instance.EnsureLoaded ();
            Cirrious.MvvmCross.Plugins.Json.PluginLoader.Instance.EnsureLoaded ();

			InitializeSocialNetwork ();
		}

		private void InitializeSocialNetwork ()
		{
			TinyIoCContainer.Current.Register<IFacebookService,FacebookService> ();

			TinyIoCContainer.Current.Register<ITwitterService> ((c, p) => 
            {
				var settings = c.Resolve<IAppSettings> ();
				var oauthConfig = new OAuthConfig ();
				if (settings.Data.TwitterEnabled) 
                {
					oauthConfig = new OAuthConfig 
                    {
						ConsumerKey = settings.Data.TwitterConsumerKey,
						Callback = settings.Data.TwitterCallback,
						ConsumerSecret = settings.Data.TwitterConsumerSecret,
						RequestTokenUrl = settings.Data.TwitterRequestTokenUrl,
						AccessTokenUrl = settings.Data.TwitterAccessTokenUrl,
						AuthorizeUrl = settings.Data.TwitterAuthorizeUrl 
					};

				}
				var twitterService = new TwitterService (oauthConfig, () => Mvx.Resolve<UINavigationController> ());
				return twitterService; 
			});
		}

		protected override Cirrious.MvvmCross.Touch.Views.Presenters.IMvxTouchViewPresenter CreatePresenter ()
		{
			return new PhonePresenter ((UIApplicationDelegate)ApplicationDelegate, base.Window);
		}

		protected override Cirrious.CrossCore.IoC.IMvxIoCProvider CreateIocProvider ()
		{
			return new TinyIoCProvider (TinyIoCContainer.Current);
		}
	}
}
