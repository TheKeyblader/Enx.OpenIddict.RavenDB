using Enx.OpenIddict.RavenDB.Indexes;
using Enx.OpenIddict.RavenDB.Models;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace Enx.OpenIddict.RavenDB
{
    public class OpenIddictRavenDBApplicationStore<TApplication>(IAsyncDocumentSession session) : IOpenIddictApplicationStore<TApplication>
        where TApplication : OpenIddictRavenDBApplication
    {
        protected IAsyncDocumentSession Session { get; } = session;

        public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
        {
            return await Session.Query<TApplication>().CountAsync(cancellationToken);
        }

        public virtual async ValueTask<long> CountAsync<TResult>(Func<IQueryable<TApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await query(Session.Query<TApplication>()).CountAsync(cancellationToken);
        }

        public virtual async ValueTask CreateAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            await Session.StoreAsync(application, cancellationToken);
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual async ValueTask DeleteAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            var changeVector = Session.Advanced.GetChangeVectorFor(application);
            Session.Delete(application.Id, changeVector);
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual async ValueTask<TApplication?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(identifier);

            return await Session.Query<TApplication, ApplicationIndex>().FirstOrDefaultAsync(a => a.ClientId == identifier, cancellationToken);
        }

        public virtual async ValueTask<TApplication?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(identifier);

            return await Session.LoadAsync<TApplication>(identifier, cancellationToken);
        }

        public virtual IAsyncEnumerable<TApplication> FindByPostLogoutRedirectUriAsync(
            [StringSyntax(StringSyntaxAttribute.Uri)] string address, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(address);

            var query = Session.Query<TApplication, ApplicationIndex>()
                .Where(a => a.PostLogoutRedirectUris.Contains(address));
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TApplication> FindByRedirectUriAsync(
            [StringSyntax(StringSyntaxAttribute.Uri)] string address, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(address);

            var query = Session.Query<TApplication, ApplicationIndex>()
                .Where(a => a.RedirectUris.Contains(address));
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public ValueTask<string?> GetApplicationTypeAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            return new(application.ApplicationType);
        }

        public virtual async ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<TApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await query(Session.Query<TApplication>(), state).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual ValueTask<string?> GetClientIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            return new ValueTask<string?>(application.ClientId);
        }

        public virtual ValueTask<string?> GetClientSecretAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            return new ValueTask<string?>(application.ClientSecret);
        }

        public virtual ValueTask<string?> GetClientTypeAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            return new ValueTask<string?>(application.Type);
        }

        public virtual ValueTask<string?> GetConsentTypeAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            return new ValueTask<string?>(application.ConsentType);
        }

        public virtual ValueTask<string?> GetDisplayNameAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            return new ValueTask<string?>(application.DisplayName);
        }

        public virtual ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (application.DisplayNames is null || application.DisplayNames.Count == 0)
            {
                return new ValueTask<ImmutableDictionary<CultureInfo, string>>(ImmutableDictionary.Create<CultureInfo, string>());
            }

            return new ValueTask<ImmutableDictionary<CultureInfo, string>>(application.DisplayNames.ToImmutableDictionary());
        }

        public virtual ValueTask<string?> GetIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            return new ValueTask<string?>(application.Id);
        }

        public ValueTask<JsonWebKeySet?> GetJsonWebKeySetAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (application.JsonWebKeySet is null)
                return new(result: null);

            return new(JsonWebKeySet.Create(application.JsonWebKeySet));
        }

        public virtual ValueTask<ImmutableArray<string>> GetPermissionsAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (application.Permissions is null || application.Permissions.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>([]);
            }

            return new ValueTask<ImmutableArray<string>>([.. application.Permissions]);
        }

        public virtual ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (application.PostLogoutRedirectUris is null || application.PostLogoutRedirectUris.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>([]);
            }

            return new ValueTask<ImmutableArray<string>>([.. application.PostLogoutRedirectUris]);
        }

        public virtual ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

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
            ArgumentNullException.ThrowIfNull(application);

            if (application.RedirectUris is null || application.RedirectUris.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>([]);
            }

            return new ValueTask<ImmutableArray<string>>([.. application.RedirectUris]);
        }

        public virtual ValueTask<ImmutableArray<string>> GetRequirementsAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (application.Requirements is null || application.Requirements.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>([]);
            }

            return new ValueTask<ImmutableArray<string>>([.. application.Requirements]);
        }

        public ValueTask<ImmutableDictionary<string, string>> GetSettingsAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (application.Settings.Count == 0)
                return new(result: []);
            return new(application.Settings.ToImmutableDictionary());
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
            ArgumentNullException.ThrowIfNull(query);

            return Session.ToAsyncEnumerable((IRavenQueryable<TResult>)query(Session.Query<TApplication>(), state), cancellationToken);
        }

        public ValueTask SetApplicationTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.ApplicationType = type;
            return default;
        }

        public virtual ValueTask SetClientIdAsync(TApplication application, string? identifier, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.ClientId = identifier;
            return default;
        }

        public virtual ValueTask SetClientSecretAsync(TApplication application, string? secret, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.ClientSecret = secret;
            return default;
        }

        public virtual ValueTask SetClientTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.Type = type;
            return default;
        }

        public virtual ValueTask SetConsentTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.ConsentType = type;
            return default;
        }

        public virtual ValueTask SetDisplayNameAsync(TApplication application, string? name, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.DisplayName = name;
            return default;
        }

        public virtual ValueTask SetDisplayNamesAsync(TApplication application, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.DisplayNames = names;
            return default;
        }

        public ValueTask SetJsonWebKeySetAsync(TApplication application, JsonWebKeySet? set, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.JsonWebKeySet = set is not null ? JsonSerializer.Serialize(set, OpenIddictSerializer.Default.JsonWebKeySet) : null;
            return default;
        }

        public virtual ValueTask SetPermissionsAsync(TApplication application, ImmutableArray<string> permissions, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (permissions.IsDefaultOrEmpty)
            {
                application.Permissions = [];

                return default;
            }

            application.Permissions = [.. permissions];
            return default;
        }

        public virtual ValueTask SetPostLogoutRedirectUrisAsync(TApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (addresses.IsDefaultOrEmpty)
            {
                application.PostLogoutRedirectUris = [];
                return default;
            }

            application.PostLogoutRedirectUris = [.. addresses];
            return default;
        }

        public virtual ValueTask SetPropertiesAsync(TApplication application, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

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
            ArgumentNullException.ThrowIfNull(application);

            if (addresses.IsDefaultOrEmpty)
            {
                application.RedirectUris = [];

                return default;
            }

            application.RedirectUris = [.. addresses];
            return default;
        }

        public virtual ValueTask SetRequirementsAsync(TApplication application, ImmutableArray<string> requirements, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            if (requirements.IsDefaultOrEmpty)
            {
                application.Requirements = [];

                return default;
            }

            application.Requirements = [.. requirements];
            return default;
        }

        public ValueTask SetSettingsAsync(TApplication application, ImmutableDictionary<string, string> settings, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            application.Settings = settings;
            return default;
        }

        public virtual async ValueTask UpdateAsync(TApplication application, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(application);

            var changeVector = Session.Advanced.GetChangeVectorFor(application);
            await Session.StoreAsync(application, changeVector, application.Id, cancellationToken);
            await Session.SaveChangesAsync(cancellationToken);
        }
    }
}
