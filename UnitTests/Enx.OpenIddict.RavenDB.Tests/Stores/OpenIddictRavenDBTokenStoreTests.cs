using Enx.OpenIddict.RavenDB.Models;
using System.Collections.Immutable;
using System.Text.Json;

namespace Enx.OpenIddict.RavenDB.Tests;

public class OpenIddictRavenDBTokenStoreTests : RavenBaseTest
{
    [Fact]
    public async Task Should_IncreaseCount_When_CountingTokensAfterCreatingOne()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Subject = Guid.NewGuid().ToString() };
        var beforeCount = await tokenStore.CountAsync(CancellationToken.None);
        await tokenStore.CreateAsync(token, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var count = await tokenStore.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(beforeCount + 1, count);
    }

    [Fact]
    public async Task Should_ReturnCount_When_CountingTokensBasedOnLinq()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var subject = Guid.NewGuid().ToString();
        await tokenStore.CreateAsync(new OpenIddictRavenDBToken { Subject = subject }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var count = await tokenStore.CountAsync(
            x => x.Where(t => t.Subject == subject),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Should_CreateToken_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Subject = Guid.NewGuid().ToString() };

        // Act
        await tokenStore.CreateAsync(token, CancellationToken.None);

        // Assert
        Assert.NotNull(token.Id);
        var loaded = await tokenStore.FindByIdAsync(token.Id!, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(token.Subject, loaded!.Subject);
    }

    [Fact]
    public async Task Should_PatchAuthorizationTokensList_When_TokenHasAuthorizationId()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization { Status = Statuses.Valid };
        await authorizationStore.CreateAsync(authorization, CancellationToken.None);

        var token = new OpenIddictRavenDBToken
        {
            AuthorizationId = authorization.Id,
            Subject = Guid.NewGuid().ToString(),
        };

        // Act
        await tokenStore.CreateAsync(token, CancellationToken.None);

        // Assert
        Assert.NotNull(token.Id);
        using var session2 = store.OpenAsyncSession();
        var reloadedAuth = await session2.LoadAsync<OpenIddictRavenDBAuthorization>(authorization.Id!, CancellationToken.None);
        Assert.NotNull(reloadedAuth);
        Assert.Contains(token.Id!, reloadedAuth!.Tokens);
    }

    [Fact]
    public async Task Should_DeleteToken_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Subject = Guid.NewGuid().ToString() };
        await tokenStore.CreateAsync(token, CancellationToken.None);

        // Act
        await tokenStore.DeleteAsync(token, CancellationToken.None);

        // Assert
        var deleted = await tokenStore.FindByIdAsync(token.Id!, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_FindingTokensWithNoMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        WaitForIndexing(store);

        // Act
        var tokens = tokenStore.FindAsync(
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
            CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.Empty(matchedTokens);
    }

    [Fact]
    public async Task Should_ReturnMatchingTokens_When_FindingTokensWithAllFilters()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var subject = Guid.NewGuid().ToString();
        var appId = Guid.NewGuid().ToString();
        var status = Guid.NewGuid().ToString();
        var type = Guid.NewGuid().ToString();
        var tokenCount = 5;
        foreach (var _ in Enumerable.Range(0, tokenCount))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken
            {
                Subject = subject,
                ApplicationId = appId,
                Status = status,
                Type = type,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var tokens = tokenStore.FindAsync(subject, appId, status, type, CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.Equal(tokenCount, matchedTokens.Count);
    }

    [Fact]
    public async Task Should_ReturnToken_When_FindingTokensByApplicationIdWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var appId = Guid.NewGuid().ToString();
        await tokenStore.CreateAsync(new OpenIddictRavenDBToken { ApplicationId = appId }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var tokens = tokenStore.FindByApplicationIdAsync(appId, CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.Single(matchedTokens);
        Assert.Equal(appId, matchedTokens[0].ApplicationId);
    }

    [Fact]
    public async Task Should_ReturnToken_When_FindingTokensByAuthorizationIdWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var authId = Guid.NewGuid().ToString();
        await tokenStore.CreateAsync(new OpenIddictRavenDBToken { AuthorizationId = authId }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var tokens = tokenStore.FindByAuthorizationIdAsync(authId, CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.Single(matchedTokens);
        Assert.Equal(authId, matchedTokens[0].AuthorizationId);
    }

    [Fact]
    public async Task Should_ReturnNull_When_TryingToFindTokenByIdThatDoesntExist()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);

        // Act
        var token = await tokenStore.FindByIdAsync("doesnt-exist", CancellationToken.None);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task Should_ReturnToken_When_FindingTokensByIdWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Subject = Guid.NewGuid().ToString() };
        await tokenStore.CreateAsync(token, CancellationToken.None);

        // Act
        var result = await tokenStore.FindByIdAsync(token.Id!, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Subject, result!.Subject);
    }

    [Fact]
    public async Task Should_ReturnToken_When_FindingTokensByReferenceIdWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var referenceId = Guid.NewGuid().ToString();
        await tokenStore.CreateAsync(new OpenIddictRavenDBToken { ReferenceId = referenceId }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var token = await tokenStore.FindByReferenceIdAsync(referenceId, CancellationToken.None);

        // Assert
        Assert.NotNull(token);
        Assert.Equal(referenceId, token!.ReferenceId);
    }

    [Fact]
    public async Task Should_ReturnToken_When_FindingTokensBySubjectWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var subject = Guid.NewGuid().ToString();
        await tokenStore.CreateAsync(new OpenIddictRavenDBToken { Subject = subject }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var tokens = tokenStore.FindBySubjectAsync(subject, CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.Single(matchedTokens);
        Assert.Equal(subject, matchedTokens[0].Subject);
    }

    [Fact]
    public async Task Should_ReturnApplicationId_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { ApplicationId = Guid.NewGuid().ToString() };

        // Act
        var appId = await tokenStore.GetApplicationIdAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal(token.ApplicationId, appId);
    }

    [Fact]
    public async Task Should_ReturnAuthorizationId_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { AuthorizationId = Guid.NewGuid().ToString() };

        // Act
        var authId = await tokenStore.GetAuthorizationIdAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal(token.AuthorizationId, authId);
    }

    [Fact]
    public async Task Should_ReturnCreationDate_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { CreationDate = DateTime.UtcNow };

        // Act
        var creationDate = await tokenStore.GetCreationDateAsync(token, CancellationToken.None);

        // Assert
        Assert.NotNull(creationDate);
        Assert.Equal(token.CreationDate, creationDate!.Value.UtcDateTime);
    }

    [Fact]
    public async Task Should_ReturnExpirationDate_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { ExpirationDate = DateTime.UtcNow.AddHours(1) };

        // Act
        var expirationDate = await tokenStore.GetExpirationDateAsync(token, CancellationToken.None);

        // Assert
        Assert.NotNull(expirationDate);
        Assert.Equal(token.ExpirationDate, expirationDate!.Value.UtcDateTime);
    }

    [Fact]
    public async Task Should_ReturnId_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Subject = Guid.NewGuid().ToString() };
        await tokenStore.CreateAsync(token, CancellationToken.None);

        // Act
        var id = await tokenStore.GetIdAsync(token, CancellationToken.None);

        // Assert
        Assert.NotNull(id);
        Assert.Equal(token.Id, id);
    }

    [Fact]
    public async Task Should_ReturnPayload_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Payload = Guid.NewGuid().ToString() };

        // Act
        var payload = await tokenStore.GetPayloadAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal(token.Payload, payload);
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_PropertiesIsEmpty()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();

        // Act
        var properties = await tokenStore.GetPropertiesAsync(token, CancellationToken.None);

        // Assert
        Assert.Empty(properties);
    }

    [Fact]
    public async Task Should_ReturnNonEmptyDictionary_When_PropertiesExists()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        token.Properties["Test"] = true;
        token.Properties["Testing"] = "value";
        token.Properties["Testicles"] = 42;

        // Act
        var properties = await tokenStore.GetPropertiesAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal(3, properties.Count);
    }

    [Fact]
    public async Task Should_ReturnRedemptionDate_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { RedemptionDate = DateTime.UtcNow };

        // Act
        var redemptionDate = await tokenStore.GetRedemptionDateAsync(token, CancellationToken.None);

        // Assert
        Assert.NotNull(redemptionDate);
        Assert.Equal(token.RedemptionDate, redemptionDate!.Value.UtcDateTime);
    }

    [Fact]
    public async Task Should_ReturnReferenceId_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { ReferenceId = Guid.NewGuid().ToString() };

        // Act
        var referenceId = await tokenStore.GetReferenceIdAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal(token.ReferenceId, referenceId);
    }

    [Fact]
    public async Task Should_ReturnStatus_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Status = Statuses.Valid };

        // Act
        var status = await tokenStore.GetStatusAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal(Statuses.Valid, status);
    }

    [Fact]
    public async Task Should_ReturnSubject_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Subject = Guid.NewGuid().ToString() };

        // Act
        var subject = await tokenStore.GetSubjectAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal(token.Subject, subject);
    }

    [Fact]
    public async Task Should_ReturnType_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Type = TokenTypes.Bearer };

        // Act
        var type = await tokenStore.GetTypeAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal(TokenTypes.Bearer, type);
    }

    [Fact]
    public async Task Should_ReturnToken_When_GetAsyncWithLinqQuery()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var subject = Guid.NewGuid().ToString();
        await tokenStore.CreateAsync(new OpenIddictRavenDBToken { Subject = subject }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var token = await tokenStore.GetAsync<string, OpenIddictRavenDBToken>(
            (query, s) => query.Where(t => t.Subject == s),
            subject,
            CancellationToken.None);

        // Assert
        Assert.NotNull(token);
        Assert.Equal(subject, token!.Subject);
    }

    [Fact]
    public async Task Should_ReturnNewToken_When_CallingInstantiate()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);

        // Act
        var token = await tokenStore.InstantiateAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OpenIddictRavenDBToken>(token);
    }

    [Fact]
    public async Task Should_ReturnList_When_ListingTokens()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);

        var tokenCount = 10;
        var tokenIds = new List<string>();
        foreach (var index in Enumerable.Range(0, tokenCount))
        {
            var token = new OpenIddictRavenDBToken { Subject = index.ToString() };
            await tokenStore.CreateAsync(token, CancellationToken.None);
            tokenIds.Add(token.Id!);
        }
        WaitForIndexing(store);

        // Act
        var tokens = tokenStore.ListAsync(default, default, CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.True(matchedTokens.Count >= tokenCount);
        Assert.False(tokenIds.Except(matchedTokens.Select(x => x.Id!)).Any());
    }

    [Fact]
    public async Task Should_ReturnFirstFive_When_ListingTokensWithCount()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);

        foreach (var index in Enumerable.Range(0, 10))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken { Subject = index.ToString() }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var expectedCount = 5;
        var tokens = tokenStore.ListAsync(expectedCount, default, CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.Equal(expectedCount, matchedTokens.Count);
    }

    [Fact]
    public async Task Should_ReturnLastFive_When_ListingTokensWithCountAndOffset()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);

        foreach (var index in Enumerable.Range(0, 10))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken { Subject = index.ToString() }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var expectedCount = 5;
        var first = tokenStore.ListAsync(expectedCount, default, CancellationToken.None);
        var firstTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in first)
            firstTokens.Add(token);

        var tokens = tokenStore.ListAsync(expectedCount, expectedCount, CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.Equal(expectedCount, matchedTokens.Count);
        Assert.Empty(firstTokens.Select(x => x.Id!).Intersect(matchedTokens.Select(x => x.Id!)));
    }

    [Fact]
    public async Task Should_ReturnTokens_When_ListingWithLinqQuery()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var subject = Guid.NewGuid().ToString();
        await tokenStore.CreateAsync(new OpenIddictRavenDBToken { Subject = subject }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var tokens = tokenStore.ListAsync<object?, OpenIddictRavenDBToken>(
            (query, _) => query.Where(t => t.Subject == subject),
            null,
            CancellationToken.None);

        // Assert
        var matchedTokens = new List<OpenIddictRavenDBToken>();
        await foreach (var token in tokens)
            matchedTokens.Add(token);
        Assert.Single(matchedTokens);
    }

    [Fact]
    public async Task Should_DeleteAllTokens_When_AllTokensHaveExpiredAndNoAuthorization()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);

        foreach (var _ in Enumerable.Range(0, 10))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken
            {
                CreationDate = DateTime.UtcNow.AddDays(-5),
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var deleted = await tokenStore.PruneAsync(DateTimeOffset.UtcNow.AddDays(-4), CancellationToken.None);

        // Assert
        Assert.Equal(10, deleted);
    }

    [Fact]
    public async Task Should_NotDeleteAnyTokens_When_TheyAreOldButHaveValidStatusAndValidAuthorization()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        foreach (var _ in Enumerable.Range(0, 10))
        {
            var authorization = new OpenIddictRavenDBAuthorization { Status = Statuses.Valid };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken
            {
                CreationDate = DateTime.UtcNow.AddDays(-5),
                AuthorizationId = authorization.Id,
                Status = Statuses.Valid,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var deleted = await tokenStore.PruneAsync(DateTimeOffset.UtcNow.AddDays(-4), CancellationToken.None);

        // Assert
        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task Should_DeleteSomeTokens_When_SomeAreOutsideOfThresholdRange()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);

        var threshold = DateTimeOffset.UtcNow.AddDays(-5);
        // indices 0..9 days ago, no authorization → AuthorizationStatus null → condition met
        foreach (var index in Enumerable.Range(0, 10))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken
            {
                CreationDate = DateTime.UtcNow.AddDays(-index),
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act — threshold = -5 days (strict <), so indices 6,7,8,9 qualify → 4 deleted
        var deleted = await tokenStore.PruneAsync(threshold, CancellationToken.None);

        // Assert
        Assert.Equal(4, deleted);
    }

    [Fact]
    public async Task Should_RevokeMatchingTokens_When_Revoking()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var subject = $"revoke-subject-{Guid.NewGuid()}";
        var appId = $"revoke-app-{Guid.NewGuid()}";
        foreach (var _ in Enumerable.Range(0, 5))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken
            {
                Subject = subject,
                ApplicationId = appId,
            }, CancellationToken.None);
        }

        // Act
        var revokeCount = await tokenStore.RevokeAsync(subject, appId, null, null, CancellationToken.None);

        // Assert
        Assert.Equal(5, revokeCount);
        WaitForIndexing(store);
        var foundTokens = tokenStore.FindAsync(subject, appId, Statuses.Revoked, null, CancellationToken.None);
        var count = 0;
        await foreach (var token in foundTokens)
        {
            Assert.Equal(Statuses.Revoked, token.Status);
            count++;
        }
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task Should_RevokeAllMatches_When_RevokingByApplicationId()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var appId = $"revoke-app-{Guid.NewGuid()}";
        foreach (var _ in Enumerable.Range(0, 5))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken { ApplicationId = appId }, CancellationToken.None);
        }

        // Act
        var revokeCount = await tokenStore.RevokeByApplicationIdAsync(appId, CancellationToken.None);

        // Assert
        Assert.Equal(5, revokeCount);
        WaitForIndexing(store);
        var tokens = tokenStore.FindByApplicationIdAsync(appId, CancellationToken.None);
        await foreach (var token in tokens)
            Assert.Equal(Statuses.Revoked, token.Status);
    }

    [Fact]
    public async Task Should_RevokeAllMatches_When_RevokingByAuthorizationId()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var authId = $"revoke-auth-{Guid.NewGuid()}";
        foreach (var _ in Enumerable.Range(0, 5))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken { AuthorizationId = authId }, CancellationToken.None);
        }

        // Act
        var revokeCount = await tokenStore.RevokeByAuthorizationIdAsync(authId, CancellationToken.None);

        // Assert
        Assert.Equal(5, revokeCount);
        WaitForIndexing(store);
        var tokens = tokenStore.FindByAuthorizationIdAsync(authId, CancellationToken.None);
        await foreach (var token in tokens)
            Assert.Equal(Statuses.Revoked, token.Status);
    }

    [Fact]
    public async Task Should_RevokeAllMatches_When_RevokingBySubject()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var subject = $"revoke-subject-{Guid.NewGuid()}";
        foreach (var _ in Enumerable.Range(0, 5))
        {
            await tokenStore.CreateAsync(new OpenIddictRavenDBToken { Subject = subject }, CancellationToken.None);
        }

        // Act
        var revokeCount = await tokenStore.RevokeBySubjectAsync(subject, CancellationToken.None);

        // Assert
        Assert.Equal(5, revokeCount);
        WaitForIndexing(store);
        var tokens = tokenStore.FindBySubjectAsync(subject, CancellationToken.None);
        await foreach (var token in tokens)
            Assert.Equal(Statuses.Revoked, token.Status);
    }

    [Fact]
    public async Task Should_SetApplicationId_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var value = Guid.NewGuid().ToString();

        // Act
        await tokenStore.SetApplicationIdAsync(token, value, CancellationToken.None);

        // Assert
        Assert.Equal(value, token.ApplicationId);
    }

    [Fact]
    public async Task Should_SetAuthorizationId_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var value = Guid.NewGuid().ToString();

        // Act
        await tokenStore.SetAuthorizationIdAsync(token, value, CancellationToken.None);

        // Assert
        Assert.Equal(value, token.AuthorizationId);
    }

    [Fact]
    public async Task Should_SetCreationDate_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var value = DateTimeOffset.UtcNow;

        // Act
        await tokenStore.SetCreationDateAsync(token, value, CancellationToken.None);

        // Assert
        Assert.Equal(value.UtcDateTime, token.CreationDate);
    }

    [Fact]
    public async Task Should_SetExpirationDate_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var value = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        await tokenStore.SetExpirationDateAsync(token, value, CancellationToken.None);

        // Assert
        Assert.Equal(value.UtcDateTime, token.ExpirationDate);
    }

    [Fact]
    public async Task Should_SetPayload_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var value = Guid.NewGuid().ToString();

        // Act
        await tokenStore.SetPayloadAsync(token, value, CancellationToken.None);

        // Assert
        Assert.Equal(value, token.Payload);
    }

    [Fact]
    public async Task Should_SetEmptyProperties_When_SetEmptyDictionaryAsProperties()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();

        // Act
        await tokenStore.SetPropertiesAsync(token, ImmutableDictionary<string, JsonElement>.Empty, CancellationToken.None);

        // Assert
        Assert.Empty(token.Properties);
    }

    [Fact]
    public async Task Should_SetProperties_When_SettingProperties()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var properties = new Dictionary<string, JsonElement>
        {
            { "Test", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            { "Testing", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            { "Testicles", JsonDocument.Parse("{ \"Test\": true }").RootElement },
        };

        // Act
        await tokenStore.SetPropertiesAsync(token, properties.ToImmutableDictionary(), CancellationToken.None);

        // Assert
        Assert.Equal(3, token.Properties.Count);
    }

    [Fact]
    public async Task Should_SetRedemptionDate_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var value = DateTimeOffset.UtcNow;

        // Act
        await tokenStore.SetRedemptionDateAsync(token, value, CancellationToken.None);

        // Assert
        Assert.Equal(value.UtcDateTime, token.RedemptionDate);
    }

    [Fact]
    public async Task Should_SetReferenceId_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var value = Guid.NewGuid().ToString();

        // Act
        await tokenStore.SetReferenceIdAsync(token, value, CancellationToken.None);

        // Assert
        Assert.Equal(value, token.ReferenceId);
    }

    [Fact]
    public async Task Should_SetStatus_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();

        // Act
        await tokenStore.SetStatusAsync(token, Statuses.Valid, CancellationToken.None);

        // Assert
        Assert.Equal(Statuses.Valid, token.Status);
    }

    [Fact]
    public async Task Should_SetSubject_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();
        var value = Guid.NewGuid().ToString();

        // Act
        await tokenStore.SetSubjectAsync(token, value, CancellationToken.None);

        // Assert
        Assert.Equal(value, token.Subject);
    }

    [Fact]
    public async Task Should_SetType_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken();

        // Act
        await tokenStore.SetTypeAsync(token, TokenTypes.Bearer, CancellationToken.None);

        // Assert
        Assert.Equal(TokenTypes.Bearer, token.Type);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToUpdateTokenThatIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await tokenStore.UpdateAsync(default!, CancellationToken.None));
    }

    [Fact]
    public async Task Should_UpdateToken_When_TokenIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var tokenStore = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session);
        var token = new OpenIddictRavenDBToken { Subject = Guid.NewGuid().ToString() };
        await tokenStore.CreateAsync(token, CancellationToken.None);

        // Act
        token.Subject = "updated-subject";
        await tokenStore.UpdateAsync(token, CancellationToken.None);

        // Assert
        var loaded = await tokenStore.FindByIdAsync(token.Id!, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("updated-subject", loaded!.Subject);
    }

    [Fact]
    public async Task Should_ThrowException_When_ConcurrencyConflictOnTokenUpdate()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session1 = store.OpenAsyncSession();
        var tokenStore1 = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session1);
        var token = new OpenIddictRavenDBToken { Subject = Guid.NewGuid().ToString() };
        await tokenStore1.CreateAsync(token, CancellationToken.None);
        var tokenId = token.Id!;

        using var session2 = store.OpenAsyncSession();
        var tokenStore2 = new OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>(session2);
        var token2 = await tokenStore2.FindByIdAsync(tokenId, CancellationToken.None);
        token2!.Subject = "session2-update";
        await tokenStore2.UpdateAsync(token2, CancellationToken.None);

        // Act & Assert — session1's change vector is now stale
        token.Subject = "session1-update";
        await Assert.ThrowsAsync<Raven.Client.Exceptions.ConcurrencyException>(async () =>
            await tokenStore1.UpdateAsync(token, CancellationToken.None));
    }
}
