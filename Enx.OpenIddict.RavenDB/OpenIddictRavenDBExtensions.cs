using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Enx.OpenIddict.RavenDB;
using Enx.OpenIddict.RavenDB.Models;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Exposes extensions allowing to register the OpenIddict MongoDB services.
    /// </summary>
    public static class OpenIddictRavenDBExtensions
    {
        /// <summary>
        /// Registers the MongoDB stores services in the DI container and
        /// configures OpenIddict to use the MongoDB entities by default.
        /// </summary>
        /// <param name="builder">The services builder used by OpenIddict to register new services.</param>
        /// <remarks>This extension can be safely called multiple times.</remarks>
        /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
        public static OpenIddictRavenDBBuilder UseRavenDB(this OpenIddictCoreBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Note: Mongo uses simple binary comparison checks by default so the additional
            // query filtering applied by the default OpenIddict managers can be safely disabled.
            builder.DisableAdditionalFiltering();

            builder.SetDefaultApplicationEntity<OpenIddictRavenDBApplication>()
                   .SetDefaultAuthorizationEntity<OpenIddictRavenDBAuthorization>()
                   .SetDefaultScopeEntity<OpenIddictRavenDBScope>()
                   .SetDefaultTokenEntity<OpenIddictRavenDBToken>();

            // Note: the Mongo stores/resolvers don't depend on scoped/transient services and thus
            // can be safely registered as singleton services and shared/reused across requests.
            builder.ReplaceApplicationStoreResolver<OpenIddictRavenDBApplicationStoreResolver>(ServiceLifetime.Scoped)
                   .ReplaceAuthorizationStoreResolver<OpenIddictRavenDBAuthorizationStoreResolver>(ServiceLifetime.Scoped)
                   .ReplaceScopeStoreResolver<OpenIddictRavenDBScopeStoreResolver>(ServiceLifetime.Scoped)
                   .ReplaceTokenStoreResolver<OpenIddictRavenDBTokenStoreResolver>(ServiceLifetime.Scoped);

            builder.Services.TryAddScoped(typeof(OpenIddictRavenDBApplicationStore<>));
            builder.Services.TryAddScoped(typeof(OpenIddictRavenDBAuthorizationStore<>));
            builder.Services.TryAddScoped(typeof(OpenIddictRavenDBScopeStore<>));
            builder.Services.TryAddScoped(typeof(OpenIddictRavenDBTokenStore<>));

            return new OpenIddictRavenDBBuilder(builder.Services);
        }

        /// <summary>
        /// Registers the MongoDB stores services in the DI container and
        /// configures OpenIddict to use the MongoDB entities by default.
        /// </summary>
        /// <param name="builder">The services builder used by OpenIddict to register new services.</param>
        /// <param name="configuration">The configuration delegate used to configure the MongoDB services.</param>
        /// <remarks>This extension can be safely called multiple times.</remarks>
        /// <returns>The <see cref="OpenIddictCoreBuilder"/>.</returns>
        public static OpenIddictCoreBuilder UseRavenDB(
            this OpenIddictCoreBuilder builder, Action<OpenIddictRavenDBBuilder> configuration)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration(builder.UseRavenDB());

            return builder;
        }
    }
}