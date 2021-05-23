using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Enx.OpenIddict.RavenDB.Indexes;
using Enx.OpenIddict.RavenDB.Models;

using OpenIddict.Abstractions;

using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace Enx.OpenIddict.RavenDB
{
    public class OpenIddictRavenDBScopeStore<TScope> : IOpenIddictScopeStore<TScope>
        where TScope : OpenIddictRavenDBScope
    {
        public OpenIddictRavenDBScopeStore(IAsyncDocumentSession session)
        {
            Session = session;
        }

        protected IAsyncDocumentSession Session { get; }

        public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
        {
            return await Session.Query<TScope>().CountAsync(cancellationToken);
        }

        public virtual async ValueTask<long> CountAsync<TResult>( Func<IQueryable<TScope>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            return await query(Session.Query<TScope>()).CountAsync(cancellationToken);
        }

        public virtual async ValueTask CreateAsync( TScope scope, CancellationToken cancellationToken)
        {
            await Session.StoreAsync(scope, cancellationToken);
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual async ValueTask DeleteAsync( TScope scope, CancellationToken cancellationToken)
        {
            var changeVector = Session.Advanced.GetChangeVectorFor(scope);
            Session.Delete(scope.Id, changeVector);
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual async ValueTask<TScope?> FindByIdAsync( string identifier, CancellationToken cancellationToken)
        {
            return await Session.LoadAsync<TScope>(identifier, cancellationToken);
        }

        public virtual async ValueTask<TScope?> FindByNameAsync( string name, CancellationToken cancellationToken)
        {
            return await Session.Query<TScope>().FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        }

        public virtual IAsyncEnumerable<TScope> FindByNamesAsync(ImmutableArray<string> names, CancellationToken cancellationToken)
        {
            if (names.Any(name => string.IsNullOrEmpty(name)))
            {
                throw new ArgumentException(SR.GetResourceString(SR.ID0203), nameof(names));
            }

            var query = Session.Query<TScope, ScopeIndex>().Where(s => Enumerable.Contains(names, s.Name));
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TScope> FindByResourceAsync( string resource, CancellationToken cancellationToken)
        {
            var query = Session.Query<TScope, ScopeIndex>().Where(s => s.Resources.Contains(resource));
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual async ValueTask<TResult> GetAsync<TState, TResult>( Func<IQueryable<TScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            return await query(Session.Query<TScope>(), state).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual ValueTask<string?> GetDescriptionAsync( TScope scope, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(scope.Description);
        }

        public virtual ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync( TScope scope, CancellationToken cancellationToken)
        {
            if (scope.Descriptions is null || scope.Descriptions.Count == 0)
            {
                return new ValueTask<ImmutableDictionary<CultureInfo, string>>(ImmutableDictionary.Create<CultureInfo, string>());
            }

            return new ValueTask<ImmutableDictionary<CultureInfo, string>>(scope.Descriptions.ToImmutableDictionary());
        }

        public virtual ValueTask<string?> GetDisplayNameAsync( TScope scope, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(scope.DisplayName);
        }

        public virtual ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync( TScope scope, CancellationToken cancellationToken)
        {
            if (scope.DisplayNames is null || scope.DisplayNames.Count == 0)
            {
                return new ValueTask<ImmutableDictionary<CultureInfo, string>>(ImmutableDictionary.Create<CultureInfo, string>());
            }

            return new ValueTask<ImmutableDictionary<CultureInfo, string>>(scope.DisplayNames.ToImmutableDictionary());
        }

        public virtual ValueTask<string?> GetIdAsync( TScope scope, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(scope.Id);
        }

        public virtual ValueTask<string?> GetNameAsync( TScope scope, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(scope.Name);
        }

        public virtual ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync( TScope scope, CancellationToken cancellationToken)
        {
            if (scope.Properties is null)
            {
                return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary.Create<string, JsonElement>());
            }

            var builder = ImmutableDictionary.CreateBuilder<string, JsonElement>();
            foreach (var keyValue in scope.Properties)
            {
                builder[keyValue.Key] = JsonExtensions.JsonElementFromObject(keyValue.Value);
            }

            return new ValueTask<ImmutableDictionary<string, JsonElement>>(builder.ToImmutable());
        }

        public virtual ValueTask<ImmutableArray<string>> GetResourcesAsync( TScope scope, CancellationToken cancellationToken)
        {
            if (scope.Resources is null || scope.Resources.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>(ImmutableArray.Create<string>());
            }

            return new ValueTask<ImmutableArray<string>>(scope.Resources.ToImmutableArray());
        }

        public virtual ValueTask<TScope> InstantiateAsync(CancellationToken cancellationToken)
        {
            try
            {
                return new ValueTask<TScope>(Activator.CreateInstance<TScope>());
            }

            catch (MemberAccessException exception)
            {
                return new ValueTask<TScope>(Task.FromException<TScope>(
                    new InvalidOperationException(SR.GetResourceString(SR.ID0246), exception)));
            }
        }

        public virtual IAsyncEnumerable<TScope> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        {
            var query = Session.Query<TScope>();

            if (offset.HasValue)
            {
                query = query.Skip(offset.Value);
            }

            if (count.HasValue)
            {
                query = query.Take(count.Value);
            }

            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TResult> ListAsync<TState, TResult>( Func<IQueryable<TScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            return Session.ToAsyncEnumerable(query(Session.Query<TScope>(), state), cancellationToken);
        }

        public virtual ValueTask SetDescriptionAsync( TScope scope, string? description, CancellationToken cancellationToken)
        {
            scope.Description = description;
            return default;
        }

        public virtual ValueTask SetDescriptionsAsync( TScope scope, ImmutableDictionary<CultureInfo, string> descriptions, CancellationToken cancellationToken)
        {
            scope.Descriptions = descriptions;
            return default;
        }

        public virtual ValueTask SetDisplayNameAsync( TScope scope, string? name, CancellationToken cancellationToken)
        {
            scope.DisplayName = name;
            return default;
        }

        public virtual ValueTask SetDisplayNamesAsync( TScope scope, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
        {
            scope.DisplayNames = names;
            return default;
        }

        public virtual ValueTask SetNameAsync( TScope scope, string? name, CancellationToken cancellationToken)
        {
            scope.Name = name;
            return default;
        }

        public virtual ValueTask SetPropertiesAsync( TScope scope, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
        {
            if (properties is null || properties.IsEmpty)
            {
                scope.Properties = ImmutableDictionary.Create<string, object>();
                return default;
            }

            var builder = ImmutableDictionary.CreateBuilder<string, object>();
            foreach (var keyValue in properties)
            {
                var value = JsonSerializer.Deserialize<object>(keyValue.Value.GetRawText());
                if (value is not null)
                    builder[keyValue.Key] = value;
            }

            scope.Properties = builder.ToImmutable();
            return default;
        }

        public virtual ValueTask SetResourcesAsync( TScope scope, ImmutableArray<string> resources, CancellationToken cancellationToken)
        {
            scope.Resources = resources;
            return default;
        }

        public virtual async ValueTask UpdateAsync( TScope scope, CancellationToken cancellationToken)
        {
            var changeVector = Session.Advanced.GetChangeVectorFor(scope);
            await Session.StoreAsync(scope, changeVector, scope.Id, cancellationToken);
            await Session.SaveChangesAsync(cancellationToken);
        }
    }
}
