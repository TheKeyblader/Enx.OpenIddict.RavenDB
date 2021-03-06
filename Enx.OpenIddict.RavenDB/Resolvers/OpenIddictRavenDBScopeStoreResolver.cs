using System;
using System.Collections.Concurrent;

using Enx.OpenIddict.RavenDB.Models;

using Microsoft.Extensions.DependencyInjection;

using OpenIddict.Abstractions;

using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace Enx.OpenIddict.RavenDB
{
    public class OpenIddictRavenDBScopeStoreResolver : IOpenIddictScopeStoreResolver
    {
        private readonly ConcurrentDictionary<Type, Type> _cache = new();
        private readonly IServiceProvider _provider;

        public OpenIddictRavenDBScopeStoreResolver(IServiceProvider provider)
            => _provider = provider;

        /// <summary>
        /// Returns a scope store compatible with the specified scope type or throws an
        /// <see cref="InvalidOperationException"/> if no store can be built using the specified type.
        /// </summary>
        /// <typeparam name="TScope">The type of the Scope entity.</typeparam>
        /// <returns>An <see cref="IOpenIddictScopeStore{TScope}"/>.</returns>
        public IOpenIddictScopeStore<TScope> Get<TScope>() where TScope : class
        {
            var store = _provider.GetService<IOpenIddictScopeStore<TScope>>();
            if (store is not null)
            {
                return store;
            }

            var type = _cache.GetOrAdd(typeof(TScope), key =>
            {
                if (!typeof(OpenIddictRavenDBScope).IsAssignableFrom(key))
                {
                    throw new InvalidOperationException(SR.GetResourceString(SR.ID0259));
                }

                return typeof(OpenIddictRavenDBScopeStore<>).MakeGenericType(key);
            });

            return (IOpenIddictScopeStore<TScope>)_provider.GetRequiredService(type);
        }
    }
}
