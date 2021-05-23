using System;
using System.Collections.Concurrent;

using Enx.OpenIddict.RavenDB.Models;

using Microsoft.Extensions.DependencyInjection;

using OpenIddict.Abstractions;

using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace Enx.OpenIddict.RavenDB
{
    public class OpenIddictRavenDBAuthorizationStoreResolver : IOpenIddictAuthorizationStoreResolver
    {
        private readonly ConcurrentDictionary<Type, Type> _cache = new();
        private readonly IServiceProvider _provider;

        public OpenIddictRavenDBAuthorizationStoreResolver(IServiceProvider provider)
            => _provider = provider;

        /// <summary>
        /// Returns an authorization store compatible with the specified authorization type or throws an
        /// <see cref="InvalidOperationException"/> if no store can be built using the specified type.
        /// </summary>
        /// <typeparam name="TAuthorization">The type of the Authorization entity.</typeparam>
        /// <returns>An <see cref="IOpenIddictAuthorizationStore{TAuthorization}"/>.</returns>
        public IOpenIddictAuthorizationStore<TAuthorization> Get<TAuthorization>() where TAuthorization : class
        {
            var store = _provider.GetService<IOpenIddictAuthorizationStore<TAuthorization>>();
            if (store is not null)
            {
                return store;
            }

            var type = _cache.GetOrAdd(typeof(TAuthorization), key =>
            {
                if (!typeof(OpenIddictRavenDBAuthorization).IsAssignableFrom(key))
                {
                    throw new InvalidOperationException(SR.GetResourceString(SR.ID0258));
                }

                return typeof(OpenIddictRavenDBAuthorizationStore<>).MakeGenericType(key);
            });

            return (IOpenIddictAuthorizationStore<TAuthorization>)_provider.GetRequiredService(type);
        }
    }
}
