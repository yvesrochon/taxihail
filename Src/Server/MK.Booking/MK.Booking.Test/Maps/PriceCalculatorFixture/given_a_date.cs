﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using apcurium.MK.Booking.Maps.Impl;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Provider;
using NUnit.Framework;

#endregion

namespace apcurium.MK.Booking.Test.Maps.PriceCalculatorFixture
{
    [TestFixture]
    public class given_a_day_tariff_ending_the_same_day
    {
        [SetUp]
        public void Setup()
        {
            var tariff1 = new Tariff
            {
                Name = "Day tariff ending the same day",
                StartTime = new DateTime(2012, 12, 17, 8, 0, 0),
                EndTime = new DateTime(2012, 12, 17, 20, 0, 0),
                Type = (int) TariffType.Day, // Monday
                VehicleTypeId = 10
            };

            var tariff2 = new Tariff
            {
                Name = "Default Day tariff ending the same day for all vehicles",
                StartTime = new DateTime(2012, 12, 17, 8, 0, 0),
                EndTime = new DateTime(2012, 12, 17, 20, 0, 0),
                Type = (int)TariffType.Day, // Monday
                VehicleTypeId = null
            };

            _tariffProvider = new FakeTariffProvider(new[]
            {
                tariff1, tariff2
            });
            _sut = new PriceCalculator(_tariffProvider, new Logger());
        }

        private PriceCalculator _sut;
        private FakeTariffProvider _tariffProvider;

        [Test]
        public void when_date_doesnt_match_day()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 3, 21, 0, 0));

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_and_end_time_exactly()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 17, 20, 0, 0));

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_and_inside_time_period()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 17, 9, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Day tariff ending the same day", tariff.Name);
        }

        [Test]
        public void when_date_match_day_and_start_time_exactly()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 17, 8, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Day tariff ending the same day", tariff.Name);
        }

        [Test]
        public void when_date_match_day_but_after_end_time()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 17, 21, 0, 0), 10);

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_but_before_start_time()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 17, 6, 0, 0), 10);

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_and_start_time_exactly_for_all_vehicles()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 17, 8, 0, 0));
            Assert.NotNull(tariff);
            Assert.AreEqual("Default Day tariff ending the same day for all vehicles", tariff.Name);
        }
    }

    [TestFixture]
    public class given_a_day_tariff_ending_the_next_day
    {
        [SetUp]
        public void Setup()
        {
            var tariff1 = new Tariff
            {
                Name = "Day tariff ending the next day",
                StartTime = new DateTime(2012, 12, 18, 20, 0, 0),
                EndTime = new DateTime(2012, 12, 19, 8, 0, 0),
                Type = (int) TariffType.Day, //Tuesday
                VehicleTypeId = 10
            };

            _tariffProvider = new FakeTariffProvider(new[]
            {
                tariff1
            });
            _sut = new PriceCalculator(_tariffProvider, new Logger());
        }

        private PriceCalculator _sut;
        private FakeTariffProvider _tariffProvider;

        [Test]
        public void when_date_doesnt_match_day()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 3, 21, 0, 0), 10);

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_and_inside_time_period()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 18, 21, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Day tariff ending the next day", tariff.Name);
        }

        [Test]
        public void when_date_match_day_and_start_time_exactly()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 18, 20, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Day tariff ending the next day", tariff.Name);
        }

        [Test]
        public void when_date_match_day_but_before_start_time()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 18, 19, 0, 0), 10);

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_next_day_and_end_time_exactly()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 19, 8, 0, 0), 10);

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_next_day_and_inside_time_period()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 19, 1, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Day tariff ending the next day", tariff.Name);
        }

        [Test]
        public void when_date_match_next_day_but_after_end_time()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 19, 21, 0, 0), 10);

            Assert.Null(tariff);
        }
    }

    [TestFixture]
// ReSharper disable once InconsistentNaming
    public class given_a_recurring_tariff_ending_the_same_day
    {
        [SetUp]
        public void Setup()
        {
            var tariff1 = new Tariff
            {
                Name = "Recurring tariff ending the same day",
                StartTime = new DateTime(1900, 1, 1, 8, 0, 0),
                EndTime = new DateTime(1900, 1, 1, 20, 0, 0),
                DaysOfTheWeek = (int) (DayOfTheWeek.Thursday | DayOfTheWeek.Friday | DayOfTheWeek.Saturday),
                Type = (int) TariffType.Recurring,
                VehicleTypeId = 10
            };

            var tariff2 = new Tariff
            {
                Name = "Default Recurring tariff for all vehicle types",
                StartTime = new DateTime(1900, 1, 1, 8, 0, 0),
                EndTime = new DateTime(1900, 1, 1, 20, 0, 0),
                DaysOfTheWeek = (int)(DayOfTheWeek.Thursday | DayOfTheWeek.Friday | DayOfTheWeek.Saturday),
                Type = (int)TariffType.Recurring,
                VehicleTypeId = null
            };

            _tariffProvider = new FakeTariffProvider(new[]
            {
                tariff1, tariff2
            });
            _sut = new PriceCalculator(_tariffProvider, new Logger());
        }

        private PriceCalculator _sut;
        private FakeTariffProvider _tariffProvider;

        [Test]
        public void when_date_doesnt_match_day()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 3, 21, 0, 0));

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_and_end_time_exactly()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 20, 0, 0));

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_and_inside_time_period()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 9, 0, 0) /* Friday */, 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Recurring tariff ending the same day", tariff.Name);
        }

        [Test]
        public void when_date_match_day_and_start_time_exactly()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 8, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Recurring tariff ending the same day", tariff.Name);
        }

        [Test]
        public void when_date_match_day_but_after_end_time()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 22, 0, 0));

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_but_before_start_time()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 7, 0, 0));

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_and_start_time_exactly_for_all_vehicle()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 9, 0, 0) /* Friday */);
            Assert.NotNull(tariff);
            Assert.AreEqual("Default Recurring tariff for all vehicle types", tariff.Name);
        }
    }

    [TestFixture]
    public class given_a_recurring_tariff_ending_the_next_day
    {
        [SetUp]
        public void Setup()
        {
            var tariff1 = new Tariff
            {
                Name = "Recurring tariff ending the next day",
                StartTime = new DateTime(1900, 1, 1, 20, 0, 0),
                EndTime = new DateTime(1900, 1, 2, 8, 0, 0),
                DaysOfTheWeek = (int) (DayOfTheWeek.Friday | DayOfTheWeek.Saturday),
                Type = (int) TariffType.Recurring,
                VehicleTypeId = 10
            };

            _tariffProvider = new FakeTariffProvider(new[]
            {
                tariff1
            });
            _sut = new PriceCalculator(_tariffProvider, new Logger());
        }

        private PriceCalculator _sut;
        private FakeTariffProvider _tariffProvider;

        [Test]
        public void when_date_doesnt_match_day()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 3, 21, 0, 0), 10);

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_day_and_inside_time_period()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 21, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Recurring tariff ending the next day", tariff.Name);
        }

        [Test]
        public void when_date_match_day_and_start_time_exactly()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 20, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Recurring tariff ending the next day", tariff.Name);
        }

        [Test]
        public void when_date_match_day_but_before_start_time()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 10, 0, 0), 10);

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_next_day_and_end_time_exactly()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 22, 8, 0, 0), 10);

            Assert.Null(tariff);
        }

        [Test]
        public void when_date_match_next_day_and_inside_time_period()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 22, 7, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Recurring tariff ending the next day", tariff.Name);
        }

        [Test]
        public void when_date_match_next_day_but_after_end_time()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 22, 12, 0, 0), 10);

            Assert.Null(tariff);
        }
    }

    [TestFixture]
    public class given_a_recurring_tariff_starting_and_ending_at_midnight
    {
        [SetUp]
        public void Setup()
        {
            var tariff1 = new Tariff
            {
                Name = "Recurring tariff",
                StartTime = new DateTime(1900, 1, 1, 0, 0, 0),
                EndTime = new DateTime(1900, 1, 2, 0, 0, 0),
                DaysOfTheWeek = (int) (DayOfTheWeek.Friday | DayOfTheWeek.Saturday),
                Type = (int) TariffType.Recurring,
                VehicleTypeId = 10
            };

            _tariffProvider = new FakeTariffProvider(new[]
            {
                tariff1
            });
            _sut = new PriceCalculator(_tariffProvider, new Logger());
        }

        private PriceCalculator _sut;
        private FakeTariffProvider _tariffProvider;

        [Test]
        public void when_date_match_day_and_inside_time_period()
        {
            var tariff = _sut.GetTariffFor(new DateTime(2012, 12, 21, 21, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Recurring tariff", tariff.Name);
        }
    }

    [TestFixture]
    public class given_a_defaultVehicle_tariff_ending_the_next_day
    {
        [SetUp]
        public void Setup()
        {
            var tariff1 = new Tariff
            {
                Name = "Vehicle default tariff ending the next day",
                StartTime = new DateTime(1900, 1, 1, 20, 0, 0),
                EndTime = new DateTime(1900, 1, 2, 8, 0, 0),
                Type = (int) TariffType.VehicleDefault,
                VehicleTypeId = 10
            };

            _tariffProvider = new FakeTariffProvider(new[]
            {
                tariff1
            });
            _sut = new PriceCalculator(_tariffProvider, new Logger());
        }

        private PriceCalculator _sut;
        private FakeTariffProvider _tariffProvider;

        [Test]
        public void when_date_match_day_and_inside_time_period()
        {
            var tariff = _sut.GetTariffFor(new DateTime(1900, 1, 1, 23, 0, 0), 10);

            Assert.NotNull(tariff);
            Assert.AreEqual("Vehicle default tariff ending the next day", tariff.Name);
        }
    }

    public class FakeTariffProvider : ITariffProvider
    {
        private readonly IList<Tariff> _tariffs;

        public FakeTariffProvider(IEnumerable<Tariff> tariffs)
        {
            _tariffs = tariffs.ToList();
        }

        public IEnumerable<Tariff> GetTariffs()
        {
            return _tariffs;
        }

        public void AddRate(Tariff tariff)
        {
            _tariffs.Add(tariff);
        }
    }
}