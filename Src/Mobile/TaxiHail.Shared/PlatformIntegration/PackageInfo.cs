using Android.App;
using Android.Content;
using Android.OS;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Common.Diagnostic;
using Java.Lang;
using StringBuilder = System.Text.StringBuilder;

namespace apcurium.MK.Booking.Mobile.Client.PlatformIntegration
{
    public class PackageInfo : IPackageInfo
    {
        private static string CachedUserAgent;
        private readonly Context _appContext;
		private readonly ILogger _logger;

        public PackageInfo(Context appContext, ILogger logger)
        {
	        _appContext = appContext;
	        _logger = logger;
        }

	    public string Platform
        {
            get { return "Android"; }
        }

		public string PlatformDetails
		{
			get
			{
				return string.Format ("{0} {1} {2}",
					Build.VERSION.Release,
					Build.Manufacturer,
					Build.Model);
			}
		}

        public string Version
        {
            get
            {
                var pInfo = Application.Context.PackageManager.GetPackageInfo(_appContext.ApplicationInfo.PackageName, 0);
                return pInfo.VersionName;
            }
        }

        public string UserAgent
        {
            get
            {
	            if (CachedUserAgent != null)
	            {
		            return CachedUserAgent;
	            }

	            try
	            {
		            var result = new StringBuilder(64);
		            result.Append("Dalvik/");


		            result.Append(JavaSystem.GetProperty("java.vm.version")); // such as 1.1.0
		            result.Append(" (Linux; U; Android ");

		            var version = Build.VERSION.Release; // "1.0" or "3.4b5"
		            result.Append(version.Length > 0 ? version : "1.0");

		            // add the model for the release build
		            if (Build.VERSION.Codename == "REL")
		            {
			            var model = Build.Model;
			            if (model.Length > 0)
			            {
				            result.Append("; ");
				            result.Append(model);
			            }
		            }
		            var id = Build.Id; // "MASTER" or "M4-rc20"
		            if (id.Length > 0)
		            {
			            result.Append(" Build/");
			            result.Append(id);
		            }
		            result.Append(")");
		            CachedUserAgent = result.ToString();
	            }
	            catch(Exception ex)
	            {
					_logger.LogMessage("An error occurred while obtaining the user agent.");
					_logger.LogError(ex);

		            CachedUserAgent = "";
	            }

	            return CachedUserAgent;
            }
        }
    }
}