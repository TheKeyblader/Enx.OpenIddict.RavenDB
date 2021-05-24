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

using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;

using static OpenIddict.Abstractions.OpenIddictConstants;

using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace Enx.OpenIddict.RavenDB
{
    public class OpenIddictRavenDBTokenStore<TToken> : IOpenIddictTokenStore<TToken>
        where TToken : OpenIddictRavenDBToken
    {
        public OpenIddictRavenDBTokenStore(IAsyncDocumentSession session)
        {
            Session = session;
        }

        protected IAsyncDocumentSession Session { get; }

        public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
        {
            return await Session.Query<TToken>().CountAsync(cancellationToken);
        }

        public virtual async ValueTask<long> CountAsync<TResult>(Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            return await query(Session.Query<TToken>()).CountAsync(cancellationToken);
        }

        public virtual async ValueTask CreateAsync(TToken token, CancellationToken cancellationToken)
        {
            await Session.StoreAsync(token, cancellationToken);
            Session.Advanced.GetMetadataFor(token)[Constants.Documents.Metadata.Expires] = token.ExpirationDate;
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual async ValueTask DeleteAsync(TToken token, CancellationToken cancellationToken)
        {
            var changeVector = Session.Advanced.GetChangeVectorFor(token);
            Session.Delete(token.Id, changeVector);
            await Session.SaveChangesAsync(cancellationToken);
        }

        public virtual IAsyncEnumerable<TToken> FindAsync(string subject, string client, CancellationToken cancellationToken)
        {
            var query = Session.Query<TToken, TokenIndex>().Where(a =>
                a.Subject == subject &&
                a.ApplicationId == client);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TToken> FindAsync(string subject, string client, string status, CancellationToken cancellationToken)
        {
            var query = Session.Query<TToken, TokenIndex>().Where(a =>
                a.Subject == subject &&
                a.ApplicationId == client &&
                a.Status == status);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TToken> FindAsync(string subject, string client, string status, string type, CancellationToken cancellationToken)
        {
            var query = Session.Query<TToken, TokenIndex>().Where(a =>
                a.Subject == subject &&
                a.ApplicationId == client &&
                a.Status == status &&
                a.Type == type);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TToken> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
        {
            var query = Session.Query<TToken, TokenIndex>().Where(a =>
                    a.ApplicationId == identifier);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual IAsyncEnumerable<TToken> FindByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
        {
            var query = Session.Query<TToken, TokenIndex>().Where(a =>
                    a.AuthorizationId == identifier);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual async ValueTask<TToken?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
        {
            return await Session.LoadAsync<TToken>(identifier, cancellationToken);
        }

        public virtual async ValueTask<TToken?> FindByReferenceIdAsync(string identifier, CancellationToken cancellationToken)
        {
            return await Session.Query<TToken, TokenIndex>().FirstOrDefaultAsync(a => a.ReferenceId == identifier, cancellationToken);
        }

        public virtual IAsyncEnumerable<TToken> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            var query = Session.Query<TToken, TokenIndex>().Where(a =>
                    a.Subject == subject);
            return Session.ToAsyncEnumerable(query, cancellationToken);
        }

        public virtual ValueTask<string?> GetApplicationIdAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(token.ApplicationId);
        }

        public virtual async ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            return await query(Session.Query<TToken>(), state).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual ValueTask<string?> GetAuthorizationIdAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(token.AuthorizationId);
        }

        public virtual ValueTask<DateTimeOffset?> GetCreationDateAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<DateTimeOffset?>(token.CreationDate);
        }

        public virtual ValueTask<DateTimeOffset?> GetExpirationDateAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<DateTimeOffset?>(token.ExpirationDate);
        }

        public virtual ValueTask<string?> GetIdAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(token.Id);
        }

        public virtual ValueTask<string?> GetPayloadAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(token.Payload);
        }

        public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TToken token, CancellationToken cancellationToken)
        {
            if (token.Properties is null)
            {
                return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary.Create<string, JsonElement>());
            }

            var builder = ImmutableDictionary.CreateBuilder<string, JsonElement>();
            foreach (var keyValue in token.Properties)
            {
                builder[keyValue.Key] = JsonExtensions.JsonElementFromObject(keyValue.Value);
            }

            return new ValueTask<ImmutableDictionary<string, JsonElement>>(builder.ToImmutable());
        }

        public virtual ValueTask<DateTimeOffset?> GetRedemptionDateAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<DateTimeOffset?>(token.RedemptionDate);
        }

        public virtual ValueTask<string?> GetReferenceIdAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(token.ReferenceId);
        }

        public virtual ValueTask<string?> GetStatusAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(token.Status);
        }

        public virtual ValueTask<string?> GetSubjectAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(token.Subject);
        }

        public virtual ValueTask<string?> GetTypeAsync(TToken token, CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(token.Type);
        }

        public virtual ValueTask<TToken> InstantiateAsync(CancellationToken cancellationToken)
        {
            try
            {
                return new ValueTask<TToken>(Activator.CreateInstance<TToken>());
            }

            catch (MemberAccessException exception)
            {
                return new ValueTask<TToken>(Task.FromException<TToken>(
                    new InvalidOperationException(SR.GetResourceString(SR.ID0248), exception)));
            }
        }

        public virtual IAsyncEnumerable<TToken> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        {
            var query = Session.Query<TToken>();

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

        public virtual IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
        {
            return Session.ToAsyncEnumerable((IRavenQueryable<TResult>)query(Session.Query<TToken>(), state), cancellationToken);
        }

        public virtual async ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
        {
            var store = Session.Advanced.DocumentStore;
            var operation = await store
                .Operations
                .SendAsync(new DeleteByQueryOperation<TokenIndex.Result, AuthorizationIndex>(
                    x => x.CreationDate < threshold.UtcDateTime && (
                    (x.Status != Statuses.Inactive && x.Status != Statuses.Valid) ||
                    (x.AuthorizationStatus != Statuses.Valid))),
                    null, cancellationToken);

            await operation.WaitForCompletionAsync();
        }

        public virtual ValueTask SetApplicationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
        {
            token.ApplicationId = identifier;
            return default;
        }

        public virtual ValueTask SetAuthorizationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
        {
            if (token.AuthorizationId != null)
            {
                Session.Advanced.Patch<OpenIddictRavenDBAuthorization, string>(token.AuthorizationId, a => a.Tokens,
                    array => array.RemoveAll(id => id == token.AuthorizationId));
            }
            token.AuthorizationId = identifier;

            if (token.AuthorizationId != null)
            {
                Session.Advanced.Patch<OpenIddictRavenDBAuthorization, string>(token.AuthorizationId, a => a.Tokens,
                    array => array.Add(token.AuthorizationId));
            }

            return default;
        }

        public virtual ValueTask SetCreationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
        {
            token.CreationDate = date?.UtcDateTime;
            return default;
        }

        public virtual ValueTask SetExpirationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
        {
            token.ExpirationDate = date?.UtcDateTime;
            return default;
        }

        public virtual ValueTask SetPayloadAsync(TToken token, string? payload, CancellationToken cancellationToken)
        {
            token.Payload = payload;
            return default;
        }

        public virtual ValueTask SetPropertiesAsync(TToken token, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
        {
            if (properties is null || properties.IsEmpty)
            {
                token.Properties = ImmutableDictionary.Create<string, object>();
                return default;
            }

            var builder = ImmutableDictionary.CreateBuilder<string, object>();
            foreach (var keyValue in properties)
            {
                var value = JsonSerializer.Deserialize<object>(keyValue.Value.GetRawText());
                if (value is not null)
                    builder[keyValue.Key] = value;
            }

            token.Properties = builder.ToImmutable();
            return default;
        }

        public virtual ValueTask SetRedemptionDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
        {
            token.RedemptionDate = date?.UtcDateTime;
            return default;
        }

        public virtual ValueTask SetReferenceIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
        {
            token.ReferenceId = identifier;
            return default;
        }

        public virtual ValueTask SetStatusAsync(TToken token, string? status, CancellationToken cancellationToken)
        {
            token.Status = status;
            return default;
        }

        public virtual ValueTask SetSubjectAsync(TToken token, string? subject, CancellationToken cancellationToken)
        {
            token.Subject = subject;
            return default;
        }

        public virtual ValueTask SetTypeAsync(TToken token, string? type, CancellationToken cancellationToken)
        {
            token.Type = type;
            return default;
        }

        public virtual async ValueTask UpdateAsync(TToken token, CancellationToken cancellationToken)
        {
            var changeVector = Session.Advanced.GetChangeVectorFor(token);
            await Session.StoreAsync(token, changeVector, token.Id, cancellationToken);
            Session.Advanced.GetMetadataFor(token)[Constants.Documents.Metadata.Expires] = token.ExpirationDate;
            await Session.SaveChangesAsync(cancellationToken);
        }
    }
}
