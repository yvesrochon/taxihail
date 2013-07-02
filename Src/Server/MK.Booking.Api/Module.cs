﻿using System;
using Infrastructure.Messaging;
using AutoMapper;
using Microsoft.Practices.Unity;
using MK.Booking.Api.Client;
using apcurium.MK.Booking.Api.Client;
using apcurium.MK.Booking.Api.Client.Cmt.Payments;
using apcurium.MK.Booking.Api.Client.Cmt.Payments.BrainTree;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Api.Helpers;
using apcurium.MK.Booking.Api.Jobs;
using apcurium.MK.Booking.Api.Providers;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Common;
using apcurium.MK.Booking.ReadModel;
using apcurium.MK.Booking.ReadModel.Query;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Configuration.Impl;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Provider;

namespace apcurium.MK.Booking.Api
{
    public class Module
    {
        public void Init(IUnityContainer container)
        {
            RegisterMaps();

            container.RegisterInstance<IPopularAddressProvider>(new PopularAddressProvider(container.Resolve<IPopularAddressDao>()));
            container.RegisterInstance<ITariffProvider>(new TariffProvider(container.Resolve<ITariffDao>()));

            var paymentSettings = container.Resolve<IConfigurationManager>().GetPaymentSettings() ?? new PaymentSetting();

            switch (paymentSettings.PaymentMode)
            {
                case PaymentSetting.PaymentMethod.Braintree:
                    container.RegisterInstance<IPaymentServiceClient>(new BraintreeClient(paymentSettings.BraintreeSettings));
                    break;
                case PaymentSetting.PaymentMethod.Cmt:
                    container.RegisterInstance<IPaymentServiceClient>(new CmtPaymentClient(paymentSettings.CmtPaymentSettings));
                    break;
                case PaymentSetting.PaymentMethod.Fake:
                    container.RegisterInstance<IPaymentServiceClient>(new CmtFakeClient());
                    break;
                default:
                    //Do nothing
                    break;
            }
            
            container.RegisterType<OrderStatusUpdater, OrderStatusUpdater>();
            var mockIbsStatusUpdate = bool.Parse(container
                               .Resolve<IConfigurationManager>()
                               .GetSetting("IBS.FakeOrderStatusUpdate") ?? "false");
            if (mockIbsStatusUpdate)
            {
                container.RegisterType<IUpdateOrderStatusJob, UpdateOrderStatusJobStub>();
                container.RegisterType<OrderStatusHelper, OrderStatusIbsMock>();
            }
            else
            {
                container.RegisterType<IUpdateOrderStatusJob, UpdateOrderStatusJob>();
                container.RegisterType<OrderStatusHelper, OrderStatusHelper>();

            }
        }

        private void RegisterMaps()
        {
            var profile = new BookingApiMapperProfile();
            Mapper.AddProfile(profile);
            Mapper.AssertConfigurationIsValid(profile.ProfileName);

            Mapper.CreateMap<BookingSettingsRequest, Commands.UpdateBookingSettings>();
            Mapper.CreateMap<CreateOrder, Commands.CreateOrder>()
                .ForMember(p=> p.Id, options=> options.Ignore())
                .ForMember(p => p.OrderId, options => options.ResolveUsing(x => x.Id == Guid.Empty ? Guid.NewGuid() : x.Id));

            Mapper.CreateMap<apcurium.MK.Booking.Api.Contract.Requests.CreateOrder, Commands.SendBookingConfirmationEmail>()
                .ForMember(p => p.Id, options => options.Ignore());

            Mapper.CreateMap<PaymentSettings, Commands.CreateOrder.PaymentInformation>();
            Mapper.CreateMap<BookingSettings, Commands.SendBookingConfirmationEmail.BookingSettings>();
            Mapper.CreateMap<Address, IBSAddress>()
                .ForMember(x => x.FullAddress, y => y.ResolveUsing(a => a.BookAddress));

            Mapper.CreateMap<OrderStatusDetail, OrderStatusRequestResponse>();


            Mapper.CreateMap<RegisterAccount, Commands.RegisterAccount>()
                .ForMember(p => p.AccountId, options => options.ResolveUsing(x => x.AccountId == Guid.Empty ? Guid.NewGuid() : x.AccountId));
            Mapper.CreateMap<RegisterAccount, Commands.RegisterTwitterAccount>()
                .ForMember(p => p.AccountId, options => options.ResolveUsing(x => x.AccountId == Guid.Empty ? Guid.NewGuid() : x.AccountId)); ;
            Mapper.CreateMap<RegisterAccount, Commands.RegisterFacebookAccount>()
                .ForMember(p => p.AccountId, options => options.ResolveUsing(x => x.AccountId == Guid.Empty ? Guid.NewGuid() : x.AccountId)); ;

            Mapper.CreateMap<SaveAddress, Commands.AddFavoriteAddress>();

            Mapper.CreateMap<SaveAddress, Commands.UpdateFavoriteAddress>();

            Mapper.CreateMap<DefaultFavoriteAddress, Commands.AddDefaultFavoriteAddress>();

            Mapper.CreateMap<DefaultFavoriteAddress, Commands.UpdateDefaultFavoriteAddress>();

            Mapper.CreateMap<AccountDetail, CurrentAccountResponse>();
 

           Mapper.CreateMap<Contract.Requests.Tariff, Commands.CreateTariff>()
                .ForMember(p => p.TariffId, opt => opt.ResolveUsing(x => x.Id == Guid.Empty ? Guid.NewGuid() : x.Id))
                .ForMember(p => p.CompanyId, opt => opt.UseValue(AppConstants.CompanyId));

            Mapper.CreateMap<Contract.Requests.Tariff, Commands.UpdateTariff>()
               .ForMember(p => p.TariffId, opt => opt.ResolveUsing(x => x.Id == Guid.Empty ? Guid.NewGuid() : x.Id))
               .ForMember(p => p.CompanyId, opt => opt.UseValue(AppConstants.CompanyId));


           Mapper.CreateMap<Contract.Requests.RuleRequest, Commands.CreateRule>()
                .ForMember(p => p.RuleId, opt => opt.ResolveUsing(x => x.Id == Guid.Empty ? Guid.NewGuid() : x.Id))
                .ForMember(p => p.CompanyId, opt => opt.UseValue(AppConstants.CompanyId));

           Mapper.CreateMap<Contract.Requests.RuleRequest, Commands.UpdateRule>()
                .ForMember(p => p.RuleId, opt => opt.ResolveUsing(x => x.Id == Guid.Empty ? Guid.NewGuid() : x.Id))
                .ForMember(p => p.CompanyId, opt => opt.UseValue(AppConstants.CompanyId));

           Mapper.CreateMap<Contract.Requests.Tariff, Commands.UpdateTariff>()
               .ForMember(p => p.TariffId, opt => opt.ResolveUsing(x => x.Id == Guid.Empty ? Guid.NewGuid() : x.Id))
               .ForMember(p => p.CompanyId, opt => opt.UseValue(AppConstants.CompanyId));

           Mapper.CreateMap<Contract.Requests.RuleActivateRequest, Commands.ActivateRule>()               
               .ForMember(p => p.CompanyId, opt => opt.UseValue(AppConstants.CompanyId));

           Mapper.CreateMap<Contract.Requests.RuleDeactivateRequest, Commands.DeactivateRule>()
               .ForMember(p => p.CompanyId, opt => opt.UseValue(AppConstants.CompanyId));

            

  			AutoMapper.Mapper.CreateMap<CreditCardRequest, Commands.AddCreditCard>()
                .ForMember(x => x.CreditCardId, opt => opt.ResolveUsing(x => x.CreditCardId == Guid.Empty ? Guid.NewGuid() : x.CreditCardId));

            Mapper.CreateMap<PopularAddress, Commands.AddPopularAddress>();
            Mapper.CreateMap<PopularAddress, Commands.UpdatePopularAddress>();

        }
    }

    public class BookingApiMapperProfile :Profile
    {
        protected override void Configure()
        {
            this.CreateMap<IBSVehiclePosition, AvailableVehicle>();
        }
    }
}
