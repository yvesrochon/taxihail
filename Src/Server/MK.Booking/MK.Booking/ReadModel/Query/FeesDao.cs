﻿using System;
using System.Collections.Generic;
using System.Linq;
using apcurium.MK.Booking.Database;
using apcurium.MK.Booking.ReadModel.Query.Contract;

namespace apcurium.MK.Booking.ReadModel.Query
{
    public class FeesDao : IFeesDao
    {
        private readonly Func<BookingDbContext> _contextFactory;

        public FeesDao(Func<BookingDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public IList<FeesDetail> GetAll()
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<FeesDetail>().ToList();
            }
        }

        public FeesDetail GetMarketFees(string market)
        {
            using (var context = _contextFactory.Invoke())
            {
                return context.Query<FeesDetail>()
                    .ToList()
                    .FirstOrDefault(f => f.Market == market);
            }
        }
    }
}
