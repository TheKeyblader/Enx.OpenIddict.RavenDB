using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using OpenIddict.Abstractions;

using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

using Enx.OpenIddict.RavenDB.Indexes;
using Enx.OpenIddict.RavenDB.Models;

using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace Enx.OpenIddict.RavenDB
{
    public class OpenIddictRavenDBApplicationStore<TApplication> : IOpenIddictApplicationStore<TApplication>
        where TApplication : OpenIddictRavenDBApplication
    {
        public OpenIddictRavenDBApplicationStore(IAsyncDocumentSession session)
        {
            Session = session;
        }

        protected IAsyncDocumentSession Session { get; }

        public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
        {
            return await Session.Query<TApplication>().CountAsync(cancellationToken);
        }

        public virtual async ValueTask<long> CountAsync<TResult>(Func<IQueryable<TApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            return await query(Session.Query<TApplication>()).CountAsync(cancellationToken);
        }

        public virtual async ValueTask CreateAsync(TApplication application, CancellationToken cancellationToken)
        {
            await Session.StoreAsync(application, cancellationToken);
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual async ValueTask DeleteAsync(TApplication application, CancellationToken cancellationToken)
        {
            var changeVector = Session.Advanced.GetChangeVectorFor(application);
            Session.Delete(application.Id, changeVector);
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual async ValueTask<TApplication?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
        {
            return await Session.Query<TApplication, ApplicationIndex>().FirstOrDefaultAsync(a => a.ClientId == identifier, cancellationToken);
        }

        public virtual async ValueTask<TApplication?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
        {
            return await Session.LoadAsync<TApplication>(identifier, cancellationToken);
        }

        public virtual IAsyncEnumerable<TApplication> FindByPostLogoutRedirectUriAsync(string address, CancellationToken cancellationToken)
        {
            var query = Session.Query<TApplication, ApplicationIndex>()
                .Where(a => a.PostLogoutRedirectUris.Contains(address));
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TApplication> FindByRedirectUriAsync(string address, CancellationToken cancellationToken)
        {
            var query = Session.Query<TApplication, ApplicationIndex>()
                .Where(a => a.RedirectUris.Contains(address));
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual async ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            return await query(Session.Query<TApplication>(), state).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual ValueTask<string?> GetClientIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(application.ClientId);
        }

        public virtual ValueTask<string?> GetClientSecretAsync(TApplication application, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(application.ClientSecret);
        }

        public virtual ValueTask<string?> GetClientTypeAsync(TApplication application, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(application.Type);
        }

        public virtual ValueTask<string?> GetConsentTypeAsync(TApplication application, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(application.ConsentType);
        }

        public virtual ValueTask<string?> GetDisplayNameAsync(TApplication application, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(application.DisplayName);
        }

        public virtual ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(TApplication application, CancellationToken cancellationToken)
        {
            if (application.DisplayNames is null || application.DisplayNames.Count == 0)
            {
                return new ValueTask<ImmutableDictionary<CultureInfo, string>>(ImmutableDictionary.Create<CultureInfo, string>());
            }

            return new ValueTask<ImmutableDictionary<CultureInfo, string>>(application.DisplayNames.ToImmutableDictionary());
        }

        public virtual ValueTask<string?> GetIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(application.Id);
        }

        public virtual ValueTask<ImmutableArray<string>> GetPermissionsAsync(TApplication application, CancellationToken cancellationToken)
        {
            if (application.Permissions is null || application.Permissions.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>(ImmutableArray.Create<string>());
            }

            return new ValueTask<ImmutableArray<string>>(application.Permissions.ToImmutableArray());
        }

        public virtual ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(TApplication application, CancellationToken cancellationToken)
        {
            if (application.PostLogoutRedirectUris is null || application.PostLogoutRedirectUris.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>(ImmutableArray.Create<string>());
            }

            return new ValueTask<ImmutableArray<string>>(application.PostLogoutRedirectUris.ToImmutableArray());
        }

        public virtual ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TApplication application, CancellationToken cancellationToken)
        {
            if (application.Properties is null)
            {
                return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary.Create<string, JsonElement>());
            }

            var builder = ImmutableDictionary.CreateBuilder<string, JsonElement>();
            foreach (var keyValue in application.Properties)
            {
                builder[keyValue.Key] = JsonExtensions.JsonElementFromObject(keyValue.Value);
            }

            return new ValueTask<ImmutableDictionary<string, JsonElement>>(builder.ToImmutable());
        }

        public virtual ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(TApplication application, CancellationToken cancellationToken)
        {
            if (application.RedirectUris is null || application.RedirectUris.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>(ImmutableArray.Create<string>());
            }

            return new ValueTask<ImmutableArray<string>>(application.RedirectUris.ToImmutableArray());
        }

        public virtual ValueTask<ImmutableArray<string>> GetRequirementsAsync(TApplication application, CancellationToken cancellationToken)
        {
            if (application.Requirements is null || application.Requirements.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>(ImmutableArray.Create<string>());
            }

            return new ValueTask<ImmutableArray<string>>(application.Requirements.ToImmutableArray());
        }

        public virtual ValueTask<TApplication> InstantiateAsync(CancellationToken cancellationToken)
        {
            try
            {
                return new ValueTask<TApplication>(Activator.CreateInstance<TApplication>());
            }

            catch (MemberAccessException exception)
            {
                return new ValueTask<TApplication>(Task.FromException<TApplication>(
                    new InvalidOperationException(SR.GetResourceString(SR.ID0240), exception)));
            }
        }

        public virtual IAsyncEnumerable<TApplication> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        {
            var query = Session.Query<TApplication>();

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

        public virtual IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            return Session.ToAsyncEnumerable((IRavenQueryable<TResult>)query(Session.Query<TApplication>(), state), cancellationToken);
        }

        public virtual ValueTask SetClientIdAsync(TApplication application, string? identifier, CancellationToken cancellationToken)
        {
            application.ClientId = identifier;
            return default;
        }

        public virtual ValueTask SetClientSecretAsync(TApplication application, string? secret, CancellationToken cancellationToken)
        {
            application.ClientSecret = secret;
            return default;
        }

        public virtual ValueTask SetClientTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
        {
            application.Type = type;
            return default;
        }

        public virtual ValueTask SetConsentTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
        {
            application.ConsentType = type;
            return default;
        }

        public virtual ValueTask SetDisplayNameAsync(TApplication application, string? name, CancellationToken cancellationToken)
        {
            application.DisplayName = name;
            return default;
        }

        public virtual ValueTask SetDisplayNamesAsync(TApplication application, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
        {
            application.DisplayNames = names;
            return default;
        }

        public virtual ValueTask SetPermissionsAsync(TApplication application, ImmutableArray<string> permissions, CancellationToken cancellationToken)
        {
            if (permissions.IsDefaultOrEmpty)
            {
                application.Permissions = ImmutableList.Create<string>();

                return default;
            }

            application.Permissions = permissions.ToImmutableList();
            return default;
        }

        public virtual ValueTask SetPostLogoutRedirectUrisAsync(TApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
        {
            if (addresses.IsDefaultOrEmpty)
            {
                application.PostLogoutRedirectUris = ImmutableList.Create<string>();
                return default;
            }

            application.PostLogoutRedirectUris = addresses.ToImmutableList();
            return default;
        }

        public virtual ValueTask SetPropertiesAsync(TApplication application, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
        {
            if (properties is null || properties.IsEmpty)
            {
                application.Properties = ImmutableDictionary.Create<string, object>();
                return default;
            }

            var builder = ImmutableDictionary.CreateBuilder<string, object>();
            foreach (var keyValue in properties)
            {
                var value = JsonSerializer.Deserialize<object>(keyValue.Value.GetRawText());
                if (value is not null)
                    builder[keyValue.Key] = value;
            }

            application.Properties = builder.ToImmutable();
            return default;
        }

        public virtual ValueTask SetRedirectUrisAsync(TApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
        {
            if (addresses.IsDefaultOrEmpty)
            {
                application.RedirectUris = ImmutableList.Create<string>();

                return default;
            }

            application.RedirectUris = addresses.ToImmutableList();
            return default;
        }

        public virtual ValueTask SetRequirementsAsync(TApplication application, ImmutableArray<string> requirements, CancellationToken cancellationToken)
        {
            if (requirements.IsDefaultOrEmpty)
            {
                application.Requirements = ImmutableList.Create<string>();

                return default;
            }

            application.Requirements = requirements.ToImmutableList();
            return default;
        }

        public virtual async ValueTask UpdateAsync(TApplication application, CancellationToken cancellationToken)
        {
            var changeVector = Session.Advanced.GetChangeVectorFor(application);
            await Session.StoreAsync(application, changeVector, application.Id, cancellationToken);
            await Session.SaveChangesAsync(cancellationToken);
        }
    }
}
