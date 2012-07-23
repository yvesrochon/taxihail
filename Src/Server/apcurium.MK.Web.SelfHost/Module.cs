﻿using System.Data.Entity;
using Infrastructure;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Messaging.InMemory;
using Infrastructure.Serialization;
using Infrastructure.Sql.BlobStorage;
using Infrastructure.Sql.EventSourcing;
using Infrastructure.Sql.MessageLog;
using Microsoft.Practices.Unity;
using apcurium.MK.Booking.Database;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Configuration.Impl;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Entity;

namespace apcurium.MK.Web.SelfHost
{
    public class Module
    {
        public void Init(IUnityContainer container)
        {
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);

            RegisterInfrastructure(container);

            new MK.Common.Module().Init(container);
            new MK.Booking.Module().Init(container);
            new MK.Booking.IBS.Module().Init(container);
            new MK.Booking.Api.Module().Init(container);

            RegisterRepository(container);
            RegisterEventBus(container);
            RegisterCommandBus(container);

        }

        private void RegisterInfrastructure(IUnityContainer container)
        {
            Database.SetInitializer<EventStoreDbContext>(null);
            Database.SetInitializer<MessageLogDbContext>(null);
            Database.SetInitializer<BlobStorageDbContext>(null);

            container.RegisterInstance<ITextSerializer>(new JsonTextSerializer());
            container.RegisterInstance<IMetadataProvider>(new StandardMetadataProvider());

            // Event log database and handler.
            container.RegisterType<SqlMessageLog>(new InjectionConstructor("MessageLog", container.Resolve<ITextSerializer>(), container.Resolve<IMetadataProvider>()));
            container.RegisterType<IEventHandler, SqlMessageLogHandler>("SqlMessageLogHandler");
            container.RegisterType<ICommandHandler, SqlMessageLogHandler>("SqlMessageLogHandler");
        }

        private void RegisterCommandBus(IUnityContainer container)
        {
            var commandBus = new MemoryCommandBus();
            container.RegisterInstance<ICommandBus>(commandBus);
            container.RegisterInstance<ICommandHandlerRegistry>(commandBus);

            RegisterCommandHandlers(container);
        }

        private void RegisterEventBus(IUnityContainer container)
        {
            var eventBus = new MemoryEventBus();
            container.RegisterInstance<IEventBus>(eventBus);
            container.RegisterInstance<IEventHandlerRegistry>(eventBus);

            RegisterEventHandlers(container);
        }

        private void RegisterRepository(IUnityContainer container)
        {
            // repository
            container.RegisterType<EventStoreDbContext>(new TransientLifetimeManager(), new InjectionConstructor("EventStore"));
            container.RegisterType(typeof(IEventSourcedRepository<>), typeof(SqlEventSourcedRepository<>), new ContainerControlledLifetimeManager());
        }

        private static void RegisterCommandHandlers(IUnityContainer unityContainer)
        {
            var commandHandlerRegistry = unityContainer.Resolve<ICommandHandlerRegistry>();

            foreach (var commandHandler in unityContainer.ResolveAll<ICommandHandler>())
            {
                commandHandlerRegistry.Register(commandHandler);
            }
        }

        private static void RegisterEventHandlers(IUnityContainer unityContainer)
        {
            var commandHandlerRegistry = unityContainer.Resolve<IEventHandlerRegistry>();

            foreach (var eventHandler in unityContainer.ResolveAll<IEventHandler>())
            {
                commandHandlerRegistry.Register(eventHandler);
            }
        }
    }
}
