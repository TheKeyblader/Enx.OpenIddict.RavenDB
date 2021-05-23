using System;
using System.ComponentModel;

using Enx.OpenIddict.RavenDB.Models;

using OpenIddict.Core;

namespace Microsoft.Extensions.DependencyInjection
{
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
        /// Configures OpenIddict to use the specified entity as the default application entity.
        /// </summary>
        /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
        public OpenIddictRavenDBBuilder ReplaceDefaultApplicationEntity<TApplication>()
            where TApplication : OpenIddictRavenDBApplication
        {
            Services.Configure<OpenIddictCoreOptions>(options => options.DefaultApplicationType = typeof(TApplication));

            return this;
        }

        /// <summary>
        /// Configures OpenIddict to use the specified entity as the default authorization entity.
        /// </summary>
        /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
        public OpenIddictRavenDBBuilder ReplaceDefaultAuthorizationEntity<TAuthorization>()
            where TAuthorization : OpenIddictRavenDBAuthorization
        {
            Services.Configure<OpenIddictCoreOptions>(options => options.DefaultAuthorizationType = typeof(TAuthorization));

            return this;
        }

        /// <summary>
        /// Configures OpenIddict to use the specified entity as the default scope entity.
        /// </summary>
        /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
        public OpenIddictRavenDBBuilder ReplaceDefaultScopeEntity<TScope>()
            where TScope : OpenIddictRavenDBScope
        {
            Services.Configure<OpenIddictCoreOptions>(options => options.DefaultScopeType = typeof(TScope));

            return this;
        }

        /// <summary>
        /// Configures OpenIddict to use the specified entity as the default token entity.
        /// </summary>
        /// <returns>The <see cref="OpenIddictRavenDBBuilder"/>.</returns>
        public OpenIddictRavenDBBuilder ReplaceDefaultTokenEntity<TToken>()
            where TToken : OpenIddictRavenDBToken
        {
            Services.Configure<OpenIddictCoreOptions>(options => options.DefaultTokenType = typeof(TToken));

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
}