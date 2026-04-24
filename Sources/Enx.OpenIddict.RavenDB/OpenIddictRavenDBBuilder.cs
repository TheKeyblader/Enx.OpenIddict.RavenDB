using Enx.OpenIddict.RavenDB;
using Enx.OpenIddict.RavenDB.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using System;
using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Exposes the necessary methods required to configure the OpenIddict MongoDB services.
/// </summary>
public class OpenIddictRavenDBBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenIddictRavenDBBuilder"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    public OpenIddictRavenDBBuilder(IServiceCollection services)
        => Services = services ?? throw new ArgumentNullException(nameof(services));

    /// <summary>
    /// Gets the services collection.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IServiceCollection Services { get; }

    /// <summary>
    /// Amends the default OpenIddict MongoDB configuration.
    /// </summary>
    /// <param name="configuration">The delegate used to configure the OpenIddict options.</param>
    /// <remarks>This extension can be safely called multiple times.</remarks>
    /// <returns>The <see cref="OpenIddictRavenDBBuilder"/> instance.</returns>
    public OpenIddictRavenDBBuilder Configure(Action<OpenIddictRavenDBOptions> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Services.Configure(configuration);

        return this;
    }

    /// <summary>
    /// Configures OpenIddict to use the specified entity as the default application entity.
    /// </summary>
    /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
    public OpenIddictRavenDBBuilder ReplaceDefaultApplicationEntity<TApplication>()
        where TApplication : OpenIddictRavenDBApplication
    {
        Services.Replace(ServiceDescriptor.Scoped<IOpenIddictApplicationManager>(static provider =>
            provider.GetRequiredService<OpenIddictApplicationManager<TApplication>>()));

        Services.Replace(ServiceDescriptor.Scoped<
            IOpenIddictApplicationStore<TApplication>, OpenIddictRavenDBApplicationStore<TApplication>>());

        return this;
    }

    /// <summary>
    /// Configures OpenIddict to use the specified entity as the default authorization entity.
    /// </summary>
    /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
    public OpenIddictRavenDBBuilder ReplaceDefaultAuthorizationEntity<TAuthorization>()
        where TAuthorization : OpenIddictRavenDBAuthorization
    {
        Services.Replace(ServiceDescriptor.Scoped<IOpenIddictAuthorizationManager>(static provider =>
            provider.GetRequiredService<OpenIddictAuthorizationManager<TAuthorization>>()));

        Services.Replace(ServiceDescriptor.Scoped<
            IOpenIddictAuthorizationStore<TAuthorization>, OpenIddictRavenDBAuthorizationStore<TAuthorization>>());

        return this;
    }

    /// <summary>
    /// Configures OpenIddict to use the specified entity as the default scope entity.
    /// </summary>
    /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
    public OpenIddictRavenDBBuilder ReplaceDefaultScopeEntity<TScope>()
        where TScope : OpenIddictRavenDBScope
    {
        Services.Replace(ServiceDescriptor.Scoped<IOpenIddictScopeManager>(static provider =>
            provider.GetRequiredService<OpenIddictScopeManager<TScope>>()));

        Services.Replace(ServiceDescriptor.Scoped<
            IOpenIddictScopeStore<TScope>, OpenIddictRavenDBScopeStore<TScope>>());

        return this;
    }

    /// <summary>
    /// Configures OpenIddict to use the specified entity as the default token entity.
    /// </summary>
    /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
    public OpenIddictRavenDBBuilder ReplaceDefaultTokenEntity<TToken>()
        where TToken : OpenIddictRavenDBToken
    {
        Services.Replace(ServiceDescriptor.Scoped<IOpenIddictTokenManager>(static provider =>
            provider.GetRequiredService<OpenIddictTokenManager<TToken>>()));

        Services.Replace(ServiceDescriptor.Scoped<
            IOpenIddictTokenStore<TToken>, OpenIddictRavenDBTokenStore<TToken>>());

        return this;
    }

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => base.Equals(obj);

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => base.GetHashCode();

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string? ToString() => base.ToString();
}