using Enx.OpenIddict.RavenDB.Indexes;
using Enx.OpenIddict.RavenDB.Models;
using OpenIddict.Abstractions;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Raven.Client.Documents.Queries;
using static OpenIddict.Abstractions.OpenIddictConstants;
using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace Enx.OpenIddict.RavenDB;

public class OpenIddictRavenDBTokenStore<TToken>(
    IAsyncDocumentSession session,
    IOptionsMonitor<OpenIddictRavenDBOptions> options) : IOpenIddictTokenStore<TToken>
    where TToken : OpenIddictRavenDBToken
{
    protected IAsyncDocumentSession Session { get; } = session;
    protected IOptionsMonitor<OpenIddictRavenDBOptions> Options { get; } = options;

    public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        return await Session.Query<TToken>().CountAsync(cancellationToken);
    }

    public virtual async ValueTask<long> CountAsync<TResult>(Func<IQueryable<TToken>, IQueryable<TResult>> query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await query(Session.Query<TToken>()).CountAsync(cancellationToken);
    }

    public virtual async ValueTask CreateAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        await Session.StoreAsync(token, cancellationToken);
        //Session.Advanced.GetMetadataFor(token)[Constants.Documents.Metadata.Expires] = token.ExpirationDate;
        if (token.AuthorizationId != null)
        {
            var id = token.Id!;
            Session.Advanced.Patch<OpenIddictRavenDBAuthorization, string>(token.AuthorizationId, a => a.Tokens,
                array => array.Add(id));
        }

        await Session.SaveChangesAsync(cancellationToken);
    }

    public virtual async ValueTask DeleteAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        var changeVector = Session.Advanced.GetChangeVectorFor(token);
        Session.Delete(token.Id, changeVector);
        if (token.AuthorizationId != null)
        {
            Session.Advanced.Patch<OpenIddictRavenDBAuthorization, string>(token.AuthorizationId, a => a.Tokens,
                array => array.RemoveAll(x => x == token.Id));
        }

        await Session.SaveChangesAsync(cancellationToken);
    }

    public virtual IAsyncEnumerable<TToken> FindAsync(string? subject, string? client, string? status, string? type,
        CancellationToken cancellationToken)
    {
        var query = TokenQuery();

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(t => t.Subject == subject);

        if (!string.IsNullOrEmpty(client))
            query = query.Where(t => t.ApplicationId == client);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.Type == type);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        return Session.ToAsyncEnumerable(query, cancellationToken);
    }

    public virtual IAsyncEnumerable<TToken> FindByApplicationIdAsync(string identifier,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        var query = TokenQuery().Where(a =>
            a.ApplicationId == identifier);
        return Session.ToAsyncEnumerable(query, cancellationToken);
    }

    public virtual IAsyncEnumerable<TToken> FindByAuthorizationIdAsync(string identifier,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        var query = TokenQuery().Where(a =>
            a.AuthorizationId == identifier);
        return Session.ToAsyncEnumerable(query, cancellationToken);
    }

    public virtual async ValueTask<TToken?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        return await Session.LoadAsync<TToken>(identifier, cancellationToken);
    }

    public virtual async ValueTask<TToken?> FindByReferenceIdAsync(string identifier,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        return await TokenQuery()
            .FirstOrDefaultAsync(a => a.ReferenceId == identifier, cancellationToken);
    }

    public virtual IAsyncEnumerable<TToken> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);

        var query = TokenQuery().Where(a =>
            a.Subject == subject);
        return Session.ToAsyncEnumerable(query, cancellationToken);
    }

    public virtual ValueTask<string?> GetApplicationIdAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<string?>(token.ApplicationId);
    }

    public virtual async ValueTask<TResult?> GetAsync<TState, TResult>(
        Func<IQueryable<TToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await query(Session.Query<TToken>(), state).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual ValueTask<string?> GetAuthorizationIdAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<string?>(token.AuthorizationId);
    }

    public virtual ValueTask<DateTimeOffset?> GetCreationDateAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<DateTimeOffset?>(token.CreationDate);
    }

    public virtual ValueTask<DateTimeOffset?> GetExpirationDateAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<DateTimeOffset?>(token.ExpirationDate);
    }

    public virtual ValueTask<string?> GetIdAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<string?>(token.Id);
    }

    public virtual ValueTask<string?> GetPayloadAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<string?>(token.Payload);
    }

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TToken token,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.Properties is null)
        {
            return new ValueTask<ImmutableDictionary<string, JsonElement>>(ImmutableDictionary
                .Create<string, JsonElement>());
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
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<DateTimeOffset?>(token.RedemptionDate);
    }

    public virtual ValueTask<string?> GetReferenceIdAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<string?>(token.ReferenceId);
    }

    public virtual ValueTask<string?> GetStatusAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<string?>(token.Status);
    }

    public virtual ValueTask<string?> GetSubjectAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        return new ValueTask<string?>(token.Subject);
    }

    public virtual ValueTask<string?> GetTypeAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

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

    public virtual IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
        Func<IQueryable<TToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return Session.ToAsyncEnumerable((IRavenQueryable<TResult>)query(Session.Query<TToken>(), state),
            cancellationToken);
    }

    public virtual async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        var store = Session.Advanced.DocumentStore;

        if (Options.CurrentValue.UseStaticIndexes)
        {
            var operation = await store
                .Operations
                .SendAsync(new DeleteByQueryOperation<TokenIndex<TToken>.Result, TokenIndex<TToken>>(x =>
                        x.CreationDate < threshold.UtcDateTime && (
                            (x.Status != Statuses.Inactive && x.Status != Statuses.Valid) ||
                            (x.AuthorizationStatus != Statuses.Valid))),
                    null, cancellationToken);

            var result = await operation.WaitForCompletionAsync<BulkOperationResult>();
            return result.Total;
        }
        else
        {
            var query = from t in TokenQuery()
                where t.CreationDate < threshold.UtcDateTime
                where (t.Status != Statuses.Inactive && t.Status != Statuses.Valid)
                let a = RavenQuery.Load<OpenIddictRavenDBAuthorization>(t.AuthorizationId)
                select new { Id = t.Id, Status = a.Status };

            var tokens = await query.ToArrayAsync(cancellationToken);
            foreach (var token in tokens)
                if (token.Status != Statuses.Valid)
                    Session.Delete(token.Id);
            await Session.SaveChangesAsync(cancellationToken);
            return tokens.Length;
        }
    }

    public async ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type,
        CancellationToken cancellationToken)
    {
        var query = Session.Query<TToken>();

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(a => a.Subject == subject);

        if (!string.IsNullOrEmpty(client))
            query = query.Where(a => a.ApplicationId == client);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(a => a.Type == type);

        var tokens = await query.ToArrayAsync(cancellationToken);
        foreach (var auth in tokens)
            auth.Status = Statuses.Revoked;
        await Session.SaveChangesAsync(cancellationToken);
        return tokens.Length;
    }

    public async ValueTask<long> RevokeByApplicationIdAsync(string identifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        var query = Session.Query<TToken>()
            .Where(x => x.ApplicationId == identifier);

        var tokens = await query.ToArrayAsync(cancellationToken);
        foreach (var auth in tokens)
            auth.Status = Statuses.Revoked;
        await Session.SaveChangesAsync(cancellationToken);
        return tokens.Length;
    }

    public async ValueTask<long> RevokeByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        var query = Session.Query<TToken>()
            .Where(x => x.AuthorizationId == identifier);

        var tokens = await query.ToArrayAsync(cancellationToken);
        foreach (var auth in tokens)
            auth.Status = Statuses.Revoked;
        await Session.SaveChangesAsync(cancellationToken);
        return tokens.Length;
    }

    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);

        var query = Session.Query<TToken>()
            .Where(x => x.Subject == subject);

        var tokens = await query.ToArrayAsync(cancellationToken);
        foreach (var auth in tokens)
            auth.Status = Statuses.Revoked;
        await Session.SaveChangesAsync(cancellationToken);
        return tokens.Length;
    }

    public virtual ValueTask SetApplicationIdAsync(TToken token, string? identifier,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.ApplicationId = identifier;
        return default;
    }

    public virtual ValueTask SetAuthorizationIdAsync(TToken token, string? identifier,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.AuthorizationId = identifier;
        return default;
    }

    public virtual ValueTask SetCreationDateAsync(TToken token, DateTimeOffset? date,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.CreationDate = date?.UtcDateTime;
        return default;
    }

    public virtual ValueTask SetExpirationDateAsync(TToken token, DateTimeOffset? date,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.ExpirationDate = date?.UtcDateTime;
        return default;
    }

    public virtual ValueTask SetPayloadAsync(TToken token, string? payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.Payload = payload;
        return default;
    }

    public virtual ValueTask SetPropertiesAsync(TToken token, ImmutableDictionary<string, JsonElement> properties,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

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

    public virtual ValueTask SetRedemptionDateAsync(TToken token, DateTimeOffset? date,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.RedemptionDate = date?.UtcDateTime;
        return default;
    }

    public virtual ValueTask SetReferenceIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.ReferenceId = identifier;
        return default;
    }

    public virtual ValueTask SetStatusAsync(TToken token, string? status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.Status = status;
        return default;
    }

    public virtual ValueTask SetSubjectAsync(TToken token, string? subject, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.Subject = subject;
        return default;
    }

    public virtual ValueTask SetTypeAsync(TToken token, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.Type = type;
        return default;
    }

    public virtual async ValueTask UpdateAsync(TToken token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);

        var changeVector = Session.Advanced.GetChangeVectorFor(token);
        await Session.StoreAsync(token, changeVector, token.Id, cancellationToken);
        //Session.Advanced.GetMetadataFor(token)[Constants.Documents.Metadata.Expires] = token.ExpirationDate;
        await Session.SaveChangesAsync(cancellationToken);
    }

    public IRavenQueryable<TToken> TokenQuery()
    {
        return Options.CurrentValue.UseStaticIndexes
            ? Session.Query<TToken, TokenIndex<TToken>>()
            : Session.Query<TToken>();
    }
}