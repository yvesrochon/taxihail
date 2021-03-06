﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apcurium.MK.Common.Extensions
{
    public static class UserAgentExtension
    {
        /// <summary>
        /// Extracts human readible Operating system name.
        /// </summary>
        /// <param name="userAgent">User Agent string from Request.</param>
        /// <returns>Human readible Operating system name.</returns>
        public static string GetOperatingSystem(this string userAgent)
        {
            var clientOsName = string.Empty;

            if (string.IsNullOrWhiteSpace(userAgent))
            {
                clientOsName = "Unknown OS";
            }
            else if (userAgent.Contains("Windows 98"))
                clientOsName = "Windows 98";
            else if (userAgent.Contains("Windows NT 5.0"))
                clientOsName = "Windows 2000";
            else if (userAgent.Contains("Windows NT 5.1"))
                clientOsName = "Windows XP";
            else if (userAgent.Contains("Windows NT 6.0"))
                clientOsName = "Windows Vista";
            else if (userAgent.Contains("Windows NT 6.1"))
                clientOsName = "Windows 7";
            else if (userAgent.Contains("Windows NT 6.2"))
                clientOsName = "Windows 8";
            else if (userAgent.Contains("Windows"))
            {
                clientOsName = GetOsVersion(userAgent, "Windows");
            }
            else if (userAgent.Contains("Android"))
            {
                clientOsName = GetOsVersion(userAgent, "Android");
            }
            else if (userAgent.Contains("Linux"))
            {
                clientOsName = GetOsVersion(userAgent, "Linux");
            }
            else if (userAgent.Contains("iPhone"))
            {
                clientOsName = GetOsVersion(userAgent, "iPhone");
            }
            else if (userAgent.Contains("iPad"))
            {
                clientOsName = GetOsVersion(userAgent, "iPad");
            }
            else if (userAgent.Contains("Macintosh"))
            {
                clientOsName = GetOsVersion(userAgent, "Macintosh");
            }
            else
            {
                clientOsName = "Unknown OS";
            }

            return clientOsName;
        }

        public static bool IsFromWebApp(this string userAgent)
        {
            throw new NotImplementedException();
        }

        private static string GetOsVersion(string userAgent, string osName)
        {
            if (userAgent.Split(new[] { osName }, StringSplitOptions.None)[1].Split(new[] { ';', ')' }).Length != 0)
            {
                return string.Format("{0}{1}", osName, userAgent.Split(new[] { osName }, StringSplitOptions.None)[1].Split(new[] { ';', ')' })[0]);
            }

            return osName;
        }
    }


}
