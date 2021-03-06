﻿#region

using System.Collections.Generic;
using apcurium.MK.Common.Enumeration;

#endregion

namespace apcurium.MK.Booking.PushNotifications
{
    public interface IPushNotificationService
    {
        void Send(string alert, IDictionary<string, object> data, string deviceToken,
            PushNotificationServicePlatform platform);
    }
}