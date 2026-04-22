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
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;
using SR = OpenIddict.Abstractions.OpenIddictResources;

namespace Enx.OpenIddict.RavenDB;

public class OpenIddictRavenDBAuthorizationStore<TAuthorization>(IAsyncDocumentSession session) : IOpenIddictAuthorizationStore<TAuthorization>
    where TAuthorization : OpenIddictRavenDBAuthorization
{
    protected IAsyncDocumentSession Session { get; } = session;

    public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        return await Session.Query<TAuthorization>().CountAsync(cancellationToken);
    }

    public virtual async ValueTask<long> CountAsync<TResult>(Func<IQueryable<TAuthorization>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await query(Session.Query<TAuthorization>()).CountAsync(cancellationToken);
    }

    public virtual async ValueTask CreateAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        await Session.StoreAsync(authorization, cancellationToken);
        await Session.SaveChangesAsync(cancellationToken);
    }

    public virtual async ValueTask DeleteAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        var changeVector = Session.Advanced.GetChangeVectorFor(authorization);
        Session.Delete(authorization.Id, changeVector);
        await Session.SaveChangesAsync(cancellationToken);
    }

    public virtual IAsyncEnumerable<TAuthorization> FindAsync(string? subject, string? client, string? status, string? type, ImmutableArray<string>? scopes, CancellationToken cancellationToken)
    {
        var query = Session.Query<TAuthorization, AuthorizationIndex>();

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(a => a.Subject == subject);

        if (!string.IsNullOrEmpty(client))
            query = query.Where(a => a.ApplicationId == client);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(a => a.Type == type);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        if (scopes.HasValue)
            query = query.Where(a => a.Scopes.In(scopes.Value));

        return Session.ToAsyncEnumerable(query, cancellationToken);
    }

    public virtual IAsyncEnumerable<TAuthorization> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        return Session.ToAsyncEnumerable(Session.Query<TAuthorization, AuthorizationIndex>().Where(a => a.ApplicationId == identifier), cancellationToken);
    }

    public virtual async ValueTask<TAuthorization?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        return await Session.LoadAsync<TAuthorization>(identifier, cancellationToken);
    }

    public virtual IAsyncEnumerable<TAuthorization> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);

        return Session.ToAsyncEnumerable(Session.Query<TAuthorization, AuthorizationIndex>().Where(a => a.Subject == subject), cancellationToken);
    }

    public virtual ValueTask<string?> GetApplicationIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return new ValueTask<string?>(authorization.ApplicationId);
    }

    public virtual async ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await query(Session.Query<TAuthorization>(), state).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual ValueTask<DateTimeOffset?> GetCreationDateAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        if (authorization.CreationDate is null)
        {
            return new(result: null);
        }

        return new(DateTime.SpecifyKind(authorization.CreationDate.Value, DateTimeKind.Utc));
    }

    public virtual ValueTask<string?> GetIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return new ValueTask<string?>(authorization.Id);
    }

    public virtual ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

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
        ArgumentNullException.ThrowIfNull(authorization);

        if (authorization.Scopes is null || authorization.Scopes.Count == 0)
        {
            return new ValueTask<ImmutableArray<string>>([]);
        }

        return new ValueTask<ImmutableArray<string>>([.. authorization.Scopes]);
    }

    public virtual ValueTask<string?> GetStatusAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return new ValueTask<string?>(authorization.Status);
    }

    public virtual ValueTask<string?> GetSubjectAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return new ValueTask<string?>(authorization.Subject);
    }

    public virtual ValueTask<string?> GetTypeAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

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
        var query = Session.Query<TAuthorization>()
            .OrderBy(a => a.Id);

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
        ArgumentNullException.ThrowIfNull(query);

        return Session.ToAsyncEnumerable((IRavenQueryable<TResult>)query(Session.Query<TAuthorization>(), state), cancellationToken);
    }

    public virtual async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        var store = Session.Advanced.DocumentStore;
        var operation = await store
            .Operations
            .SendAsync(new DeleteByQueryOperation<AuthorizationIndex.Result, AuthorizationIndex>(
                x => x.CreationDate < threshold.UtcDateTime && (x.Status != Statuses.Valid || (x.Type == AuthorizationTypes.AdHoc && x.ValidTokens.Contains(true)))),
                null, cancellationToken);

        var result = await operation.WaitForCompletionAsync<BulkOperationResult>();
        return result.Total;
    }

    public async ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
    {
        var query = Session.Query<TAuthorization>();

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(a => a.Subject == subject);

        if (!string.IsNullOrEmpty(client))
            query = query.Where(a => a.ApplicationId == client);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(a => a.Type == type);

        var authorizations = await query.ToArrayAsync(cancellationToken);
        foreach (var auth in authorizations)
            auth.Status = Statuses.Revoked;
        await Session.SaveChangesAsync(cancellationToken);
        return authorizations.Length;
    }

    public async ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);


        var query = Session.Query<TAuthorization>()
            .Where(x => x.ApplicationId == identifier);

        var authorizations = await query.ToArrayAsync(cancellationToken);
        foreach (var auth in authorizations)
            auth.Status = Statuses.Revoked;
        await Session.SaveChangesAsync(cancellationToken);
        return authorizations.Length;
    }

    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);


        var query = Session.Query<TAuthorization>()
            .Where(x => x.Subject == subject);

        var authorizations = await query.ToArrayAsync(cancellationToken);
        foreach (var auth in authorizations)
            auth.Status = Statuses.Revoked;
        await Session.SaveChangesAsync(cancellationToken);
        return authorizations.Length;
    }

    public virtual ValueTask SetApplicationIdAsync(TAuthorization authorization, string? identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.ApplicationId = identifier;
        return default;
    }

    public virtual ValueTask SetCreationDateAsync(TAuthorization authorization, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.CreationDate = date?.UtcDateTime;
        return default;
    }

    public virtual ValueTask SetPropertiesAsync(TAuthorization authorization, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

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
        ArgumentNullException.ThrowIfNull(authorization);

        if (scopes.IsDefaultOrEmpty)
        {
            authorization.Scopes = [];

            return default;
        }

        authorization.Scopes = [.. scopes];
        return default;
    }

    public virtual ValueTask SetStatusAsync(TAuthorization authorization, string? status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.Status = status;
        return default;
    }

    public virtual ValueTask SetSubjectAsync(TAuthorization authorization, string? subject, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.Subject = subject;
        return default;
    }

    public virtual ValueTask SetTypeAsync(TAuthorization authorization, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.Type = type;
        return default;
    }

    public virtual async ValueTask UpdateAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        var changeVector = Session.Advanced.GetChangeVectorFor(authorization);
        await Session.StoreAsync(authorization, changeVector, authorization.Id, cancellationToken);
        await Session.SaveChangesAsync(cancellationToken);
    }
}
