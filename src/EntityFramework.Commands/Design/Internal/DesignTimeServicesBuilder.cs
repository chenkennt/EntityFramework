// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Migrations.Design;
using Microsoft.Data.Entity.Scaffolding.Internal;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Data.Entity.Design.Internal
{
    public partial class DesignTimeServicesBuilder
    {
        private readonly StartupInvoker _startup;

        public DesignTimeServicesBuilder(
            [NotNull] StartupInvoker startupInvoker)
        {
            _startup = startupInvoker;
        }

        public virtual IServiceProvider Build([NotNull] DbContext context)
        {
            Check.NotNull(context, nameof(context));

            var services = new ServiceCollection();
            ConfigureServices(services);
            ConfigureDnxServices(services);

            var contextServices = ((IInfrastructure<IServiceProvider>)context).Instance;
            ConfigureContextServices(contextServices, services);

            var databaseProviderServices = contextServices.GetRequiredService<IDatabaseProviderServices>();
            var provider = databaseProviderServices.InvariantName;
            ConfigureProviderServices(provider, services);

            ConfigureUserServices(services);

            return services.BuildServiceProvider();
        }

        public virtual IServiceProvider Build([NotNull] string provider)
        {
            Check.NotEmpty(provider, nameof(provider));

            var services = new ServiceCollection();
            ConfigureServices(services);
            ConfigureDnxServices(services);
            ConfigureProviderServices(provider, services, throwOnError: true);
            ConfigureUserServices(services);

            return services.BuildServiceProvider();
        }

        protected virtual void ConfigureServices([NotNull] IServiceCollection services)
            => services
                .AddLogging()
                .AddSingleton<CSharpHelper>()
                .AddSingleton<CSharpMigrationOperationGenerator>()
                .AddSingleton<CSharpSnapshotGenerator>()
                .AddSingleton<MigrationsCodeGenerator, CSharpMigrationsGenerator>()
                .AddScaffolding();

        partial void ConfigureDnxServices(IServiceCollection services);

#if DNX451 || DNXCORE50
        partial void ConfigureDnxServices(IServiceCollection services)
            => services.ImportDnxServices();
#endif

        private void ConfigureProviderServices(string provider, IServiceCollection services, bool throwOnError = false)
            => _startup.ConfigureDesignTimeServices(GetProviderDesignTimeServices(provider, throwOnError), services);

        protected virtual void ConfigureContextServices(
            [NotNull] IServiceProvider contextServices,
            [NotNull] IServiceCollection services)
            => services
                .AddTransient<MigrationsScaffolder>()
                .AddTransient(_ => contextServices.GetService<DbContext>())
                .AddTransient(_ => contextServices.GetService<IDatabaseProviderServices>())
                .AddTransient(_ => contextServices.GetService<IHistoryRepository>())
                .AddTransient(_ => contextServices.GetService<ILoggerFactory>())
                .AddTransient(_ => contextServices.GetService<IMigrationsAssembly>())
                .AddTransient(_ => contextServices.GetService<IMigrationsIdGenerator>())
                .AddTransient(_ => contextServices.GetService<IMigrationsModelDiffer>())
                .AddTransient(_ => contextServices.GetService<IMigrator>())
                .AddTransient(_ => contextServices.GetService<IModel>());

        private void ConfigureUserServices(IServiceCollection services)
            => _startup.ConfigureDesignTimeServices(services);

        private static Type GetProviderDesignTimeServices(string provider, bool throwOnError)
        {
            Assembly providerAssembly;
            try
            {
                providerAssembly = Assembly.Load(new AssemblyName(provider));
            }
            catch (Exception ex)
            {
                if (!throwOnError)
                {
                    return null;
                }

                throw new OperationException(CommandsStrings.CannotFindRuntimeProviderAssembly(provider), ex);
            }

            var providerServicesAttribute = providerAssembly.GetCustomAttribute<DesignTimeProviderServicesAttribute>();
            if (providerServicesAttribute == null)
            {
                if (!throwOnError)
                {
                    return null;
                }

                throw new InvalidOperationException(
                    CommandsStrings.CannotFindDesignTimeProviderAssemblyAttribute(
                        nameof(DesignTimeProviderServicesAttribute),
                        provider));
            }

            try
            {
                return Type.GetType(
                    providerServicesAttribute.FullyQualifiedTypeName,
                    throwOnError: true,
                    ignoreCase: false);
            }
            catch (Exception ex)
            when (ex is FileNotFoundException || ex is FileLoadException || ex is BadImageFormatException)
            {
                if (!throwOnError)
                {
                    return null;
                }

                throw new OperationException(
                    CommandsStrings.CannotFindDesignTimeProviderAssembly(providerServicesAttribute.PackageName), ex);
            }
        }
    }
}
