using System;
using Enx.OpenIddict.RavenDB;
using Enx.OpenIddict.RavenDB.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Exposes extensions allowing to register the OpenIddict RavenDB services.
/// </summary>
public static class OpenIddictRavenDBExtensions
{
    /// <summary>
    /// Registers the RavenDB stores services in the DI container and
    /// configures OpenIddict to use the RavenDB entities by default.
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

        builder.ReplaceApplicationStore<OpenIddictRavenDBApplication, OpenIddictRavenDBApplicationStore<OpenIddictRavenDBApplication>>()
            .ReplaceAuthorizationStore<OpenIddictRavenDBAuthorization, OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>>()
            .ReplaceScopeStore<OpenIddictRavenDBScope, OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>>()
            .ReplaceTokenStore<OpenIddictRavenDBToken, OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>>();

        return new OpenIddictRavenDBBuilder(builder.Services);
    }

    /// <summary>
    /// Registers the RavenDB stores services in the DI container and
    /// configures OpenIddict to use the RavenDB entities by default.
    /// </summary>
    /// <param name="builder">The services builder used by OpenIddict to register new services.</param>
    /// <param name="configuration">The configuration delegate used to configure the RavenDB services.</param>
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