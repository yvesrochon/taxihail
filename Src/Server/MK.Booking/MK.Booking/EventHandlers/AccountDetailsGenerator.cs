﻿using System;
using System.Linq;
using Infrastructure.Messaging.Handling;
using apcurium.MK.Booking.Events;
using apcurium.MK.Booking.Database;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Entity;

namespace apcurium.MK.Booking.EventHandlers
{
    public class AccountDetailsGenerator :
        IEventHandler<AccountRegistered>,
        IEventHandler<AccountConfirmed>,
        IEventHandler<AccountDisabled>,
        IEventHandler<AccountUpdated>,
        IEventHandler<BookingSettingsUpdated>,
        IEventHandler<AccountPasswordReset>,
        IEventHandler<AccountPasswordUpdated>,
        IEventHandler<AdminRightGranted>,
        IEventHandler<PaymentProfileUpdated>
    {
        private readonly Func<BookingDbContext> _contextFactory;
        private IConfigurationManager _configurationManager;
        public AccountDetailsGenerator(Func<BookingDbContext> contextFactory, IConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            _contextFactory = contextFactory;
        }

        public void Handle(AccountRegistered @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var account = new AccountDetail
                                 {
                                     Name = @event.Name,
                                     Email = @event.Email,
                                     Password = @event.Password,
                                     Phone = @event.Phone,
                                     Id = @event.SourceId,
                                     IBSAccountId = @event.IbsAcccountId,
                                     FacebookId = @event.FacebookId,
                                     TwitterId = @event.TwitterId,
                                     Language = @event.Language,
                                     IsAdmin = @event.IsAdmin,
                                     CreationDate = @event.EventDate,
                                     ConfirmationToken = @event.ConfirmationToken,
                                     IsConfirmed = @event.AccountActivationDisabled
                                 };

                var nbPassenger = int.Parse(_configurationManager.GetSetting("DefaultBookingSettings.NbPassenger"));
                account.Settings = new BookingSettings { Name = account.Name, NumberOfTaxi = 1, Passengers = nbPassenger, Phone = account.Phone };

                context.Save(account);
                var defaultCompanyAddress = (from a in context.Query<DefaultAddressDetails>()
                                             select a).ToList();
                
                //add default company favorite address
                defaultCompanyAddress.ForEach(c => context.Set<AddressDetails>().Add(new AddressDetails()
                {
                    AccountId = account.Id,
                    Apartment = c.Apartment,
                    BuildingName = c.BuildingName,
                    FriendlyName = c.FriendlyName,
                    FullAddress = c.FullAddress,
                    Id = Guid.NewGuid(),
                    IsHistoric = false,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    RingCode = c.RingCode
                }));
                context.SaveChanges();
            }
        }

        public void Handle(AccountConfirmed @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var account = context.Find<AccountDetail>(@event.SourceId);
                account.IsConfirmed = true;
                account.DisabledByAdmin = false;
                context.Save(account);
            }
        }

        public void Handle(AccountDisabled @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var account = context.Find<AccountDetail>(@event.SourceId);
                account.IsConfirmed = false;
                account.DisabledByAdmin = true;
                context.Save(account);
            }
        }

        public void Handle(AccountUpdated @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var account = context.Find<AccountDetail>(@event.SourceId);
                account.Name = @event.Name;

                context.Save(account);
            }
        }

        public void Handle(BookingSettingsUpdated @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var account = context.Find<AccountDetail>(@event.SourceId);
                var settings = account.Settings ?? new BookingSettings();
                settings.Name = @event.Name;

                settings.ChargeTypeId = @event.ChargeTypeId;
                settings.ProviderId = @event.ProviderId;
                settings.VehicleTypeId = @event.VehicleTypeId;

                if (settings.ChargeTypeId == ParseToNullable(_configurationManager.GetSetting("DefaultBookingSettings.ChargeTypeId")))
                {
                    settings.ChargeTypeId = null;
                }


                if (settings.VehicleTypeId == ParseToNullable(_configurationManager.GetSetting("DefaultBookingSettings.VehicleTypeId")))
                {
                    settings.VehicleTypeId = null;
                }

                if (settings.ProviderId == ParseToNullable(_configurationManager.GetSetting("DefaultBookingSettings.ProviderId")))
                {
                    settings.ProviderId = null;
                }



                settings.NumberOfTaxi = @event.NumberOfTaxi;
                settings.Passengers = @event.Passengers;
                settings.Phone = @event.Phone;

                account.Settings = settings;
                context.Save(account);
            }
        }

        private int? ParseToNullable(string val)
        {
            int result;
            if (int.TryParse(val, out result))
            {
                return result;
            }
            else
            {
                return default(int?);
            }
        }

        public void Handle(AccountPasswordReset @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var account = context.Find<AccountDetail>(@event.SourceId);
                account.Password = @event.Password;
                context.Save(account);
            }
        }
        public void Handle(AccountPasswordUpdated @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var account = context.Find<AccountDetail>(@event.SourceId);
                account.Password = @event.Password;
                context.Save(account);
            }
        }

        public void Handle(AdminRightGranted @event)
        {
            using(var context= _contextFactory.Invoke())
            {
                var account = context.Find<AccountDetail>(@event.SourceId);
                account.IsAdmin = true;
                context.Save(account);
            }
        }

        public void Handle(PaymentProfileUpdated @event)
        {
            using (var context = _contextFactory.Invoke())
            {
                var account = context.Find<AccountDetail>(@event.SourceId);
                account.DefaultCreditCard = @event.DefaultCreditCard;
                account.DefaultTipPercent = @event.DefaultTipPercent;
                context.Save(account);
            }
        }
    }
}


