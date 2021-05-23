using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    public class OpenIddictRavenDBAuthorizationStore<TAuthorization> : IOpenIddictAuthorizationStore<TAuthorization>
        where TAuthorization : OpenIddictRavenDBAuthorization
    {

        public OpenIddictRavenDBAuthorizationStore(IAsyncDocumentSession session)
        {
            Session = session;
        }

        protected IAsyncDocumentSession Session { get; }

        public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
        {
            return await Session.Query<TAuthorization>().CountAsync(cancellationToken);
        }

        public virtual async ValueTask<long> CountAsync<TResult>(Func<IQueryable<TAuthorization>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            return await query(Session.Query<TAuthorization>()).CountAsync(cancellationToken);
        }

        public virtual async ValueTask CreateAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            await Session.StoreAsync(authorization, cancellationToken);
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual ValueTask DeleteAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public virtual IAsyncEnumerable<TAuthorization> FindAsync(string subject, string client, CancellationToken cancellationToken)
        {
            var query = Session.Query<TAuthorization, AuthorizationIndex>()
                .Where(a => a.Subject == subject && a.ApplicationId == client);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TAuthorization> FindAsync(string subject, string client, string status, CancellationToken cancellationToken)
        {
            var query = Session.Query<TAuthorization, AuthorizationIndex>()
                .Where(a => a.Subject == subject && a.ApplicationId == client);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TAuthorization> FindAsync(string subject, string client, string status, string type, CancellationToken cancellationToken)
        {
            var query = Session.Query<TAuthorization, AuthorizationIndex>().Where(a =>
                a.Subject == subject &&
                a.ApplicationId == client &&
                a.Status == status &&
                a.Type == type);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TAuthorization> FindAsync(string subject, string client, string status, string type, ImmutableArray<string> scopes, CancellationToken cancellationToken)
        {
            var query = Session.Query<TAuthorization, AuthorizationIndex>().Where(a =>
                a.Subject == subject &&
                a.ApplicationId == client &&
                a.Status == status &&
                a.Type == type &&
                a.Scopes.In(scopes));
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TAuthorization> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
        {
            return Session.ToAsyncEnumerable(Session.Query<TAuthorization, AuthorizationIndex>().Where(a => a.ApplicationId == identifier), cancellationToken);
        }

        public virtual async ValueTask<TAuthorization?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
        {
            return await Session.LoadAsync<TAuthorization>(identifier, cancellationToken);
        }

        public virtual IAsyncEnumerable<TAuthorization> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            return Session.ToAsyncEnumerable(Session.Query<TAuthorization, AuthorizationIndex>().Where(a => a.Subject == subject), cancellationToken);
        }

        public virtual ValueTask<string?> GetApplicationIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(authorization.ApplicationId);
        }

        public virtual async ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            return await query(Session.Query<TAuthorization>(), state).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual ValueTask<DateTimeOffset?> GetCreationDateAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            if (authorization.CreationDate is null)
            {
                return new ValueTask<DateTimeOffset?>(result: null);
            }

            return new ValueTask<DateTimeOffset?>(DateTime.SpecifyKind(authorization.CreationDate.Value, DateTimeKind.Utc));
        }

        public virtual ValueTask<string?> GetIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(authorization.Id);
        }

        public virtual ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            if (authorization.Properties is null)
            {
                return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary.Create<string, JsonElement>());
            }

            var builder = ImmutableDictionary.CreateBuilder<string, JsonElement>();
            foreach (var keyValue in authorization.Properties)
            {
                builder[keyValue.Key] = JsonExtensions.JsonElementFromObject(keyValue.Value);
            }

            return new ValueTask<ImmutableDictionary<string, JsonElement>>(builder.ToImmutable());
        }

        public virtual ValueTask<ImmutableArray<string>> GetScopesAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            if (authorization.Scopes is null || authorization.Scopes.Count == 0)
            {
                return new ValueTask<ImmutableArray<string>>(ImmutableArray.Create<string>());
            }

            return new ValueTask<ImmutableArray<string>>(authorization.Scopes.ToImmutableArray());
        }

        public virtual ValueTask<string?> GetStatusAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(authorization.Status);
        }

        public virtual ValueTask<string?> GetSubjectAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(authorization.Subject);
        }

        public virtual ValueTask<string?> GetTypeAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(authorization.Type);
        }

        public virtual ValueTask<TAuthorization> InstantiateAsync(CancellationToken cancellationToken)
        {
            try
            {
                return new ValueTask<TAuthorization>(Activator.CreateInstance<TAuthorization>());
            }

            catch (MemberAccessException exception)
            {
                return new ValueTask<TAuthorization>(Task.FromException<TAuthorization>(
                    new InvalidOperationException(SR.GetResourceString(SR.ID0242), exception)));
            }
        }

        public virtual IAsyncEnumerable<TAuthorization> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        {
            var query = Session.Query<TAuthorization>();

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

        public virtual IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            return Session.ToAsyncEnumerable((IRavenQueryable<TResult>)query(Session.Query<TAuthorization>(), state), cancellationToken);
        }

        public virtual ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public virtual ValueTask SetApplicationIdAsync(TAuthorization authorization, string? identifier, CancellationToken cancellationToken)
        {
            authorization.ApplicationId = identifier;
            return default;
        }

        public virtual ValueTask SetCreationDateAsync(TAuthorization authorization, DateTimeOffset? date, CancellationToken cancellationToken)
        {
            authorization.CreationDate = date?.UtcDateTime;
            return default;
        }

        public virtual ValueTask SetPropertiesAsync(TAuthorization authorization, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
        {
            if (properties is null || properties.IsEmpty)
            {
                authorization.Properties = ImmutableDictionary.Create<string, object>();
                return default;
            }

            var builder = ImmutableDictionary.CreateBuilder<string, object>();
            foreach (var keyValue in properties)
            {
                var value = JsonSerializer.Deserialize<object>(keyValue.Value.GetRawText());
                if (value is not null)
                    builder[keyValue.Key] = value;
            }

            authorization.Properties = builder.ToImmutable();
            return default;
        }

        public virtual ValueTask SetScopesAsync(TAuthorization authorization, ImmutableArray<string> scopes, CancellationToken cancellationToken)
        {
            if (scopes.IsDefaultOrEmpty)
            {
                authorization.Scopes = ImmutableList.Create<string>();

                return default;
            }

            authorization.Scopes = scopes.ToImmutableList();
            return default;
        }

        public virtual ValueTask SetStatusAsync(TAuthorization authorization, string? status, CancellationToken cancellationToken)
        {
            authorization.Status = status;
            return default;
        }

        public virtual ValueTask SetSubjectAsync(TAuthorization authorization, string? subject, CancellationToken cancellationToken)
        {
            authorization.Subject = subject;
            return default;
        }

        public virtual ValueTask SetTypeAsync(TAuthorization authorization, string? type, CancellationToken cancellationToken)
        {
            authorization.Type = type;
            return default;
        }

        public virtual async ValueTask UpdateAsync(TAuthorization authorization, CancellationToken cancellationToken)
        {
            var changeVector = Session.Advanced.GetChangeVectorFor(authorization);
            await Session.StoreAsync(authorization, changeVector, authorization.Id, cancellationToken);
            await Session.SaveChangesAsync(cancellationToken);
        }
    }
}
