using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents;
using System.Collections.Immutable;
using System.Text.Json;

namespace Enx.OpenIddict.RavenDB.Tests;

public class OpenIddictRavenDBAuthorizationStoreTests : RavenBaseTest
{
    [Fact]
    public async Task Should_IncreaseCount_When_CountingAuthorizationsAfterCreatingOne()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();
        var beforeCount = await authorizationStore.CountAsync(CancellationToken.None);
        await authorizationStore.CreateAsync(authorization, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var count = await authorizationStore.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(beforeCount + 1, count);
    }

    [Fact]
    public async Task Should_ReturnCount_When_CountingAuthorizationsBasedOnLinq()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var subject = Guid.NewGuid().ToString();
        await authorizationStore.CreateAsync(
            new OpenIddictRavenDBAuthorization { Subject = subject },
            CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var count = await authorizationStore.CountAsync(
            x => x.Where(a => a.Subject == subject),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToCreateAuthorizationThatIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.CreateAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_CreateAuthorization_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization
        {
            ApplicationId = Guid.NewGuid().ToString(),
        };

        // Act
        await authorizationStore.CreateAsync(authorization, CancellationToken.None);

        // Assert
        Assert.NotNull(authorization.Id);
        var loaded = await authorizationStore.FindByIdAsync(authorization.Id!, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(authorization.ApplicationId, loaded!.ApplicationId);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToDeleteAuthorizationThatIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.DeleteAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_DeleteAuthorization_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization
        {
            ApplicationId = Guid.NewGuid().ToString(),
        };
        await authorizationStore.CreateAsync(authorization, CancellationToken.None);

        // Act
        await authorizationStore.DeleteAsync(authorization, CancellationToken.None);

        // Assert
        var deletedAuthorization = await authorizationStore.FindByIdAsync(authorization.Id!, CancellationToken.None);
        Assert.Null(deletedAuthorization);
    }

    [Fact]
    public async Task Should_ReturnAuthorizations_When_ListingWithLinqQuery()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var subject = Guid.NewGuid().ToString();
        await authorizationStore.CreateAsync(
            new OpenIddictRavenDBAuthorization { Subject = subject },
            CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.ListAsync<object?, OpenIddictRavenDBAuthorization>(
            (query, _) => query.Where(a => a.Subject == subject),
            null,
            CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var auth in authorizations)
        {
            matchedAuthorizations.Add(auth);
        }
        Assert.Single(matchedAuthorizations);
        Assert.Equal(subject, matchedAuthorizations[0].Subject);
    }

    [Fact]
    public async Task Should_ReturnList_When_ListingAuthorizations()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var authorizationCount = 10;
        var authorizationIds = new List<string>();
        foreach (var index in Enumerable.Range(0, authorizationCount))
        {
            var authorization = new OpenIddictRavenDBAuthorization { Subject = index.ToString() };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);
            authorizationIds.Add(authorization.Id!);
        }
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.ListAsync(default, default, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Equal(authorizationCount, matchedAuthorizations.Count);
        Assert.False(authorizationIds.Except(matchedAuthorizations.Select(x => x.Id!)).Any());
    }

    [Fact]
    public async Task Should_ReturnFirstFive_When_ListingAuthorizationsWithCount()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(
                new OpenIddictRavenDBAuthorization { Subject = index.ToString() },
                CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var expectedCount = 5;
        var authorizations = authorizationStore.ListAsync(expectedCount, default, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Equal(expectedCount, matchedAuthorizations.Count);
    }

    [Fact]
    public async Task Should_ReturnLastFive_When_ListingAuthorizationsWithCountAndOffset()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(
                new OpenIddictRavenDBAuthorization { Subject = index.ToString() },
                CancellationToken.None);
        }
        WaitForIndexing(store);

        var pageSize = 5;

        // Act
        var firstPage = authorizationStore.ListAsync(pageSize, default, CancellationToken.None);
        var firstAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in firstPage)
        {
            firstAuthorizations.Add(authorization);
        }

        var secondPage = authorizationStore.ListAsync(pageSize, pageSize, CancellationToken.None);
        var secondAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in secondPage)
        {
            secondAuthorizations.Add(authorization);
        }

        // Assert
        Assert.Equal(pageSize, secondAuthorizations.Count);
        Assert.Empty(firstAuthorizations.Select(x => x.Id).Intersect(secondAuthorizations.Select(x => x.Id)));
    }

    [Fact]
    public async Task Should_ThrowException_When_ToUpdateAuthorizationThatIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.UpdateAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ThrowConcurrencyException_When_ConcurrentUpdateOccurs()
    {
        // Arrange
        using var store = GetDocumentStore();

        using var session1 = store.OpenAsyncSession();
        var authorizationStore1 = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session1);
        var authorization = new OpenIddictRavenDBAuthorization();
        await authorizationStore1.CreateAsync(authorization, CancellationToken.None);

        using var session2 = store.OpenAsyncSession();
        var authorizationStore2 = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session2);
        var authInSession2 = await authorizationStore2.FindByIdAsync(authorization.Id!, CancellationToken.None);
        authInSession2!.Subject = "Modified by session 2";
        await authorizationStore2.UpdateAsync(authInSession2, CancellationToken.None);

        // Act & Assert
        authorization.Subject = "Modified by session 1";
        await Assert.ThrowsAsync<Raven.Client.Exceptions.ConcurrencyException>(async () =>
            await authorizationStore1.UpdateAsync(authorization, CancellationToken.None));
    }

    [Fact]
    public async Task Should_UpdateAuthorization_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();
        await authorizationStore.CreateAsync(authorization, CancellationToken.None);

        // Act
        authorization.Subject = "testing-to-update";
        await authorizationStore.UpdateAsync(authorization, CancellationToken.None);

        // Assert
        var updatedAuthorization = await authorizationStore.FindByIdAsync(authorization.Id!, CancellationToken.None);
        Assert.NotNull(updatedAuthorization);
        Assert.Equal(authorization.Subject, updatedAuthorization!.Subject);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetTypeAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.SetTypeAsync(default!, default, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetType_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var type = "SomeType";
        await authorizationStore.SetTypeAsync(authorization, type, CancellationToken.None);

        // Assert
        Assert.Equal(type, authorization.Type);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetSubjectAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.SetSubjectAsync(default!, default, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetSubject_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var subject = "SomeSubject";
        await authorizationStore.SetSubjectAsync(authorization, subject, CancellationToken.None);

        // Assert
        Assert.Equal(subject, authorization.Subject);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetStatusAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.SetStatusAsync(default!, default, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetStatus_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var status = "SomeStatus";
        await authorizationStore.SetStatusAsync(authorization, status, CancellationToken.None);

        // Assert
        Assert.Equal(status, authorization.Status);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetScopesAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.SetScopesAsync(default!, default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyListAsScopes()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        await authorizationStore.SetScopesAsync(authorization, default, CancellationToken.None);

        // Assert
        Assert.Empty(authorization.Scopes);
    }

    [Fact]
    public async Task Should_SetScopes_When_SettingScopes()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var scopes = new List<string> { "something", "other", "some_more" };
        await authorizationStore.SetScopesAsync(
            authorization,
            scopes.ToImmutableArray(),
            CancellationToken.None);

        // Assert
        Assert.NotNull(authorization.Scopes);
        Assert.Equal(3, authorization.Scopes!.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPropertiesAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.SetPropertiesAsync(default!, default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyDictionaryAsProperties()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        await authorizationStore.SetPropertiesAsync(
            authorization,
            ImmutableDictionary.Create<string, JsonElement>(),
            CancellationToken.None);

        // Assert
        Assert.Empty(authorization.Properties);
    }

    [Fact]
    public async Task Should_SetProperties_When_SettingProperties()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var properties = new Dictionary<string, JsonElement>
        {
            { "Test", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            { "Testing", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            { "Testicles", JsonDocument.Parse("{ \"Test\": true }").RootElement },
        };
        await authorizationStore.SetPropertiesAsync(
            authorization,
            properties.ToImmutableDictionary(x => x.Key, x => x.Value),
            CancellationToken.None);

        // Assert
        Assert.NotNull(authorization.Properties);
        Assert.Equal(3, authorization.Properties!.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPropertiesAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.GetPropertiesAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_PropertiesIsEmpty()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var properties = await authorizationStore.GetPropertiesAsync(authorization, CancellationToken.None);

        // Assert
        Assert.Empty(properties);
    }

    [Fact]
    public async Task Should_ReturnNonEmptyDictionary_When_PropertiesExists()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization
        {
            Properties = new Dictionary<string, object>
            {
                { "Test", new { Something = true } },
                { "Testing", new { Something = true } },
                { "Testicles", new { Something = true } },
            },
        };

        // Act
        var properties = await authorizationStore.GetPropertiesAsync(authorization, CancellationToken.None);

        // Assert
        Assert.NotNull(properties);
        Assert.Equal(3, properties.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetCreationDateAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.SetCreationDateAsync(default!, default, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetCreationDate_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var creationDate = DateTimeOffset.UtcNow;
        await authorizationStore.SetCreationDateAsync(authorization, creationDate, CancellationToken.None);

        // Assert
        Assert.Equal(creationDate.UtcDateTime, authorization.CreationDate);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetApplicationIdAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.SetApplicationIdAsync(default!, default, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetApplicationId_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var applicationId = Guid.NewGuid().ToString();
        await authorizationStore.SetApplicationIdAsync(authorization, applicationId, CancellationToken.None);

        // Assert
        Assert.Equal(applicationId, authorization.ApplicationId);
    }

    [Fact]
    public async Task Should_ReturnNewAuthorization_When_CallingInstantiate()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act
        var authorization = await authorizationStore.InstantiateAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OpenIddictRavenDBAuthorization>(authorization);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetTypeAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.GetTypeAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnType_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization { Type = "SomeType" };

        // Act
        var type = await authorizationStore.GetTypeAsync(authorization, CancellationToken.None);

        // Assert
        Assert.NotNull(type);
        Assert.Equal(authorization.Type, type);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetSubjectAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.GetSubjectAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnSubject_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization { Subject = "SomeSubject" };

        // Act
        var subject = await authorizationStore.GetSubjectAsync(authorization, CancellationToken.None);

        // Assert
        Assert.NotNull(subject);
        Assert.Equal(authorization.Subject, subject);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetStatusAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.GetStatusAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnStatus_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization { Status = "SomeStatus" };

        // Act
        var status = await authorizationStore.GetStatusAsync(authorization, CancellationToken.None);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(authorization.Status, status);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetIdAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.GetIdAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnId_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();
        await authorizationStore.CreateAsync(authorization, CancellationToken.None);

        // Act
        var id = await authorizationStore.GetIdAsync(authorization, CancellationToken.None);

        // Assert
        Assert.NotNull(id);
        Assert.Equal(authorization.Id, id);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetCreationDateAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.GetCreationDateAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnCreationDate_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var utcNow = DateTime.UtcNow;
        var authorization = new OpenIddictRavenDBAuthorization { CreationDate = utcNow };

        // Act
        var creationDate = await authorizationStore.GetCreationDateAsync(authorization, CancellationToken.None);

        // Assert
        Assert.NotNull(creationDate);
        Assert.Equal(utcNow, creationDate!.Value.UtcDateTime);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetApplicationIdAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.GetApplicationIdAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnApplicationId_When_AuthorizationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization
        {
            ApplicationId = Guid.NewGuid().ToString(),
        };

        // Act
        var applicationId = await authorizationStore.GetApplicationIdAsync(authorization, CancellationToken.None);

        // Assert
        Assert.NotNull(applicationId);
        Assert.Equal(authorization.ApplicationId, applicationId);
    }

    [Fact]
    public async Task Should_ReturnAuthorization_When_GetAsyncIsCalledWithValidQuery()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var subject = Guid.NewGuid().ToString();
        await authorizationStore.CreateAsync(
            new OpenIddictRavenDBAuthorization { Subject = subject },
            CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var result = await authorizationStore.GetAsync<object?, OpenIddictRavenDBAuthorization>(
            (query, _) => query.Where(a => a.Subject == subject),
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(subject, result!.Subject);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetScopesAndAuthorizationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.GetScopesAsync(default!, CancellationToken.None));
        Assert.Equal("authorization", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_AuthorizationDoesntHaveAnyScopes()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();

        // Act
        var scopes = await authorizationStore.GetScopesAsync(authorization, CancellationToken.None);

        // Assert
        Assert.Empty(scopes);
    }

    [Fact]
    public async Task Should_ReturnScopes_When_AuthorizationHasScopes()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization
        {
            Scopes = ["get", "set", "delete"],
        };

        // Act
        var scopes = await authorizationStore.GetScopesAsync(authorization, CancellationToken.None);

        // Assert
        Assert.Equal(3, scopes.Length);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_FindingAuthorizationsWithNoMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act
        var authorizations = authorizationStore.FindAsync("test", "test", null, null, null, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Empty(matchedAuthorizations);
    }

    [Fact]
    public async Task Should_ReturnListAll_When_FindingAuthorizationsWithoutParameters()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var suffix = Guid.NewGuid().ToString();
        var count = 10;
        foreach (var index in Enumerable.Range(0, count))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = $"{index}-{suffix}",
                ApplicationId = index.ToString(),
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.FindAsync(null, null, null, null, null, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            if (authorization.Subject?.EndsWith(suffix) == true)
                matchedAuthorizations.Add(authorization);
        }
        Assert.Equal(count, matchedAuthorizations.Count);
    }

    [Fact]
    public async Task Should_ReturnListOfOne_When_FindingAuthorizationsWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var uniqueKey = Guid.NewGuid().ToString();
        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = $"{index}-{uniqueKey}",
                ApplicationId = $"{index}-{uniqueKey}",
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.FindAsync(
            $"5-{uniqueKey}", $"5-{uniqueKey}", null, null, null, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Single(matchedAuthorizations);
    }

    [Fact]
    public async Task Should_ReturnListOfOne_When_FindingAuthorizationsWithStatusMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var status = "some-status";
        var uniqueKey = Guid.NewGuid().ToString();
        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = $"{index}-{uniqueKey}",
                ApplicationId = $"{index}-{uniqueKey}",
                Status = status,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.FindAsync(
            $"5-{uniqueKey}", $"5-{uniqueKey}", status, null, null, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Single(matchedAuthorizations);
    }

    [Fact]
    public async Task Should_ReturnListOfOne_When_FindingAuthorizationsWithTypeMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var status = "some-status";
        var type = "some-type";
        var uniqueKey = Guid.NewGuid().ToString();
        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = $"{index}-{uniqueKey}",
                ApplicationId = $"{index}-{uniqueKey}",
                Status = status,
                Type = type,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.FindAsync(
            $"5-{uniqueKey}", $"5-{uniqueKey}", status, type, null, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Single(matchedAuthorizations);
    }

    [Fact]
    public async Task Should_ReturnListOfOne_When_FindingAuthorizationsWithScopesMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var status = "some-status";
        var type = "some-type";
        var scopes = new List<string> { "get" };
        var uniqueKey = Guid.NewGuid().ToString();
        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = $"{index}-{uniqueKey}",
                ApplicationId = $"{index}-{uniqueKey}",
                Status = status,
                Type = type,
                Scopes = scopes,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.FindAsync(
            $"5-{uniqueKey}",
            $"5-{uniqueKey}",
            status,
            type,
            scopes.ToImmutableArray(),
            CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Single(matchedAuthorizations);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindAuthorizationByApplicationIdAndIdentifierIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            authorizationStore.FindByApplicationIdAsync(default!, CancellationToken.None));
        Assert.Equal("identifier", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_FindingAuthorizationsByApplicationIdWithNoMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act
        var authorizations = authorizationStore.FindByApplicationIdAsync(
            Guid.NewGuid().ToString(), CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Empty(matchedAuthorizations);
    }

    [Fact]
    public async Task Should_ReturnList_When_FindingAuthorizationsByApplicationIdWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var applicationId = Guid.NewGuid().ToString();
        var authorizationCount = 10;
        foreach (var index in Enumerable.Range(0, authorizationCount))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = index.ToString(),
                ApplicationId = applicationId,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.FindByApplicationIdAsync(applicationId, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Equal(authorizationCount, matchedAuthorizations.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindAuthorizationBySubjectAndSubjectIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            authorizationStore.FindBySubjectAsync(default!, CancellationToken.None));
        Assert.Equal("subject", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_FindingAuthorizationsBySubjectWithNoMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act
        var authorizations = authorizationStore.FindBySubjectAsync(
            Guid.NewGuid().ToString(), CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Empty(matchedAuthorizations);
    }

    [Fact]
    public async Task Should_ReturnList_When_FindingAuthorizationsBySubjectWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var subject = Guid.NewGuid().ToString();
        var authorizationCount = 10;
        foreach (var index in Enumerable.Range(0, authorizationCount))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                ApplicationId = index.ToString(),
                Subject = subject,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var authorizations = authorizationStore.FindBySubjectAsync(subject, CancellationToken.None);

        // Assert
        var matchedAuthorizations = new List<OpenIddictRavenDBAuthorization>();
        await foreach (var authorization in authorizations)
        {
            matchedAuthorizations.Add(authorization);
        }
        Assert.Equal(authorizationCount, matchedAuthorizations.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindAuthorizationByIdAndIdentifierIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authorizationStore.FindByIdAsync(default!, CancellationToken.None));
        Assert.Equal("identifier", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnAuthorization_When_FindingAuthorizationsByIdWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorization = new OpenIddictRavenDBAuthorization();
        await authorizationStore.CreateAsync(authorization, CancellationToken.None);

        // Act
        var result = await authorizationStore.FindByIdAsync(authorization.Id!, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(authorization.Id, result!.Id);
    }

    [Fact]
    public async Task Should_DeleteAllAuthorizations_When_AllHasExpired()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        foreach (var _ in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                CreationDate = DateTime.UtcNow.AddDays(-5),
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var deleted = await authorizationStore.PruneAsync(DateTimeOffset.UtcNow.AddDays(-4), CancellationToken.None);

        // Assert
        Assert.Equal(10, deleted);
    }

    [Fact]
    public async Task Should_NotDeleteAnyAuthorizations_When_TheyAreOldButValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var authorizationCount = 10;

        foreach (var _ in Enumerable.Range(0, authorizationCount))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                CreationDate = DateTime.UtcNow.AddDays(-5),
                Status = Statuses.Valid,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var deleted = await authorizationStore.PruneAsync(DateTimeOffset.UtcNow.AddDays(-4), CancellationToken.None);

        // Assert
        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task Should_DeleteSomeAuthorizations_When_SomeAreOutsideOfTheThresholdRange()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);
        var threshold = DateTimeOffset.UtcNow.AddDays(-5);
        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                CreationDate = DateTime.UtcNow.AddDays(-index),
                Status = Statuses.Inactive,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);
        // Act
        var deleted = await authorizationStore.PruneAsync(threshold, CancellationToken.None);

        // Assert
        Assert.Equal(4, deleted);
    }

    [Fact]
    public async Task Should_RevokeAllMatches_When_Revoking()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var status = "some-status";
        var type = "some-type";
        var suffix = $"-revoke-all-{Guid.NewGuid()}";
        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = $"{index}{suffix}",
                ApplicationId = $"{index}{suffix}",
                Status = status,
                Type = type,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var revokeCount = await authorizationStore.RevokeAsync(
            $"5{suffix}", $"5{suffix}", status, type, CancellationToken.None);

        // Assert
        Assert.Equal(1, revokeCount);
        WaitForIndexing(store);
        var authorizations = authorizationStore.FindAsync(
            $"5{suffix}", $"5{suffix}", Statuses.Revoked, type, null, CancellationToken.None);
        var matchCount = 0;
        await foreach (var authorization in authorizations)
        {
            Assert.Equal(Statuses.Revoked, authorization.Status);
            matchCount++;
        }
        Assert.Equal(1, matchCount);
    }

    [Fact]
    public async Task Should_RevokeAllMatches_When_RevokingByApplicationId()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var status = "some-status";
        var type = "some-type";
        var suffix = $"-revoke-by-appid-{Guid.NewGuid()}";
        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = $"{index}{suffix}",
                ApplicationId = $"{index}{suffix}",
                Status = status,
                Type = type,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var revokeCount = await authorizationStore.RevokeByApplicationIdAsync(
            $"5{suffix}", CancellationToken.None);

        // Assert
        Assert.Equal(1, revokeCount);
        WaitForIndexing(store);
        var authorizations = authorizationStore.FindAsync(
            $"5{suffix}", $"5{suffix}", Statuses.Revoked, type, null, CancellationToken.None);
        var matchCount = 0;
        await foreach (var authorization in authorizations)
        {
            Assert.Equal(Statuses.Revoked, authorization.Status);
            matchCount++;
        }
        Assert.Equal(1, matchCount);
    }

    [Fact]
    public async Task Should_RevokeAllMatches_When_RevokingBySubject()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var authorizationStore = new OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>(session);

        var status = "some-status";
        var type = "some-type";
        var suffix = $"-revoke-by-subject-{Guid.NewGuid()}";
        foreach (var index in Enumerable.Range(0, 10))
        {
            await authorizationStore.CreateAsync(new OpenIddictRavenDBAuthorization
            {
                Subject = $"{index}{suffix}",
                ApplicationId = $"{index}{suffix}",
                Status = status,
                Type = type,
            }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var revokeCount = await authorizationStore.RevokeBySubjectAsync(
            $"5{suffix}", CancellationToken.None);

        // Assert
        Assert.Equal(1, revokeCount);
        WaitForIndexing(store);
        var authorizations = authorizationStore.FindAsync(
            $"5{suffix}", $"5{suffix}", Statuses.Revoked, type, null, CancellationToken.None);
        var matchCount = 0;
        await foreach (var authorization in authorizations)
        {
            Assert.Equal(Statuses.Revoked, authorization.Status);
            matchCount++;
        }
        Assert.Equal(1, matchCount);
    }
}
