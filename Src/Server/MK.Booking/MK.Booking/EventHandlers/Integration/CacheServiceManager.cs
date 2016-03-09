﻿using apcurium.MK.Booking.Events;
using apcurium.MK.Common.Caching;
using apcurium.MK.Common.Extensions;
using Infrastructure.Messaging.Handling;

namespace apcurium.MK.Booking.EventHandlers.Integration
{
    public class CacheServiceManager : IEventHandler<AllCreditCardsRemoved>
    {
        private const string UserAuthIdPattern = "\"userAuthId\":\"{0}\"";

        private readonly ICacheClient _cacheService;

        public CacheServiceManager(ICacheClient cacheService)
        {
            _cacheService = cacheService;
        }

        public void Handle(AllCreditCardsRemoved @event)
        {
            if (!@event.ForceUserDisconnect)
            {
                return;
            }

            _cacheService.RemoveByPattern(UserAuthIdPattern.InvariantCultureFormat(@event.SourceId));
        }

    }
}