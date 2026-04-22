using Enx.OpenIddict.RavenDB.Models;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

namespace Enx.OpenIddict.RavenDB.Tests;

public class OpenIddictRavenDBScopeStoreTests : RavenBaseTest
{
    [Fact]
    public async Task Should_IncreaseCount_When_CountingScopesAfterCreatingOne()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { Name = Guid.NewGuid().ToString() };
        var beforeCount = await scopeStore.CountAsync(CancellationToken.None);
        await scopeStore.CreateAsync(scope, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var count = await scopeStore.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(beforeCount + 1, count);
    }

    [Fact]
    public async Task Should_ReturnCount_When_CountingScopesBasedOnLinq()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var name = Guid.NewGuid().ToString();
        await scopeStore.CreateAsync(new OpenIddictRavenDBScope { Name = name }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var count = await scopeStore.CountAsync(
            x => x.Where(s => s.Name == name),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Should_CreateScope_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { DisplayName = Guid.NewGuid().ToString() };

        // Act
        await scopeStore.CreateAsync(scope, CancellationToken.None);

        // Assert
        Assert.NotNull(scope.Id);
        var loaded = await scopeStore.FindByIdAsync(scope.Id!, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(scope.DisplayName, loaded!.DisplayName);
    }

    [Fact]
    public async Task Should_DeleteScope_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { Name = Guid.NewGuid().ToString() };
        await scopeStore.CreateAsync(scope, CancellationToken.None);

        // Act
        await scopeStore.DeleteAsync(scope, CancellationToken.None);

        // Assert
        var deleted = await scopeStore.FindByIdAsync(scope.Id!, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Should_ReturnNull_When_TryingToFindScopeByIdThatDoesntExist()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);

        // Act
        var scope = await scopeStore.FindByIdAsync("doesnt-exist", CancellationToken.None);

        // Assert
        Assert.Null(scope);
    }

    [Fact]
    public async Task Should_ReturnScope_When_FindingScopeByIdWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { Name = Guid.NewGuid().ToString() };
        await scopeStore.CreateAsync(scope, CancellationToken.None);

        // Act
        var result = await scopeStore.FindByIdAsync(scope.Id!, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(scope.Name, result!.Name);
    }

    [Fact]
    public async Task Should_ReturnScope_When_FindingScopeByNameWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var name = $"scope-{Guid.NewGuid()}";
        await scopeStore.CreateAsync(new OpenIddictRavenDBScope { Name = name }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var scope = await scopeStore.FindByNameAsync(name, CancellationToken.None);

        // Assert
        Assert.NotNull(scope);
        Assert.Equal(name, scope!.Name);
    }

    [Fact]
    public async Task Should_ReturnNull_When_FindingScopeByNameWithNoMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        WaitForIndexing(store);

        // Act
        var scope = await scopeStore.FindByNameAsync("no-such-scope", CancellationToken.None);

        // Assert
        Assert.Null(scope);
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_FindingByNamesWithEmptyElement()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            scopeStore.FindByNamesAsync(ImmutableArray.Create<string>("valid", ""), CancellationToken.None));
    }

    [Fact]
    public async Task Should_ReturnMatchingScopes_When_FindingByNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var suffix = Guid.NewGuid().ToString();
        var names = Enumerable.Range(0, 5).Select(i => $"scope-{i}-{suffix}").ToImmutableArray();
        foreach (var name in names)
        {
            await scopeStore.CreateAsync(new OpenIddictRavenDBScope { Name = name }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var scopes = scopeStore.FindByNamesAsync(names, CancellationToken.None);

        // Assert
        var matchedScopes = new List<OpenIddictRavenDBScope>();
        await foreach (var scope in scopes)
            matchedScopes.Add(scope);
        Assert.Equal(5, matchedScopes.Count);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_FindingByNamesWithEmptyArray()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        WaitForIndexing(store);

        // Act
        var scopes = scopeStore.FindByNamesAsync(ImmutableArray<string>.Empty, CancellationToken.None);

        // Assert
        var matchedScopes = new List<OpenIddictRavenDBScope>();
        await foreach (var scope in scopes)
            matchedScopes.Add(scope);
        Assert.Empty(matchedScopes);
    }

    [Fact]
    public async Task Should_ReturnScope_When_FindingScopeByResourceWithMatch()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var resource = $"resource-{Guid.NewGuid()}";
        await scopeStore.CreateAsync(new OpenIddictRavenDBScope
        {
            Name = $"scope-{Guid.NewGuid()}",
            Resources = [resource],
        }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var scopes = scopeStore.FindByResourceAsync(resource, CancellationToken.None);

        // Assert
        var matchedScopes = new List<OpenIddictRavenDBScope>();
        await foreach (var scope in scopes)
            matchedScopes.Add(scope);
        Assert.Single(matchedScopes);
        Assert.Contains(resource, matchedScopes[0].Resources);
    }

    [Fact]
    public async Task Should_ReturnScope_When_GetAsyncWithLinqQuery()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var name = Guid.NewGuid().ToString();
        await scopeStore.CreateAsync(new OpenIddictRavenDBScope { Name = name }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var scope = await scopeStore.GetAsync<string, OpenIddictRavenDBScope>(
            (query, n) => query.Where(s => s.Name == n),
            name,
            CancellationToken.None);

        // Assert
        Assert.NotNull(scope);
        Assert.Equal(name, scope!.Name);
    }

    [Fact]
    public async Task Should_ReturnDescription_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { Description = Guid.NewGuid().ToString() };

        // Act
        var description = await scopeStore.GetDescriptionAsync(scope, CancellationToken.None);

        // Assert
        Assert.Equal(scope.Description, description);
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_ScopeDoesntHaveAnyDescriptions()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        var descriptions = await scopeStore.GetDescriptionsAsync(scope, CancellationToken.None);

        // Assert
        Assert.Empty(descriptions);
    }

    [Fact]
    public async Task Should_ReturnDescriptions_When_ScopeHasDescriptions()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope
        {
            Descriptions = new Dictionary<CultureInfo, string>
            {
                { new CultureInfo("sv-SE"), "Testar" },
                { new CultureInfo("es-ES"), "Testado" },
                { new CultureInfo("en-US"), "Testing" },
            }.ToImmutableDictionary(),
        };

        // Act
        var descriptions = await scopeStore.GetDescriptionsAsync(scope, CancellationToken.None);

        // Assert
        Assert.Equal(3, descriptions.Count);
    }

    [Fact]
    public async Task Should_ReturnDisplayName_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { DisplayName = Guid.NewGuid().ToString() };

        // Act
        var displayName = await scopeStore.GetDisplayNameAsync(scope, CancellationToken.None);

        // Assert
        Assert.Equal(scope.DisplayName, displayName);
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_ScopeDoesntHaveAnyDisplayNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        var displayNames = await scopeStore.GetDisplayNamesAsync(scope, CancellationToken.None);

        // Assert
        Assert.Empty(displayNames);
    }

    [Fact]
    public async Task Should_ReturnDisplayNames_When_ScopeHasDisplayNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope
        {
            DisplayNames = new Dictionary<CultureInfo, string>
            {
                { new CultureInfo("sv-SE"), "Testar" },
                { new CultureInfo("es-ES"), "Testado" },
                { new CultureInfo("en-US"), "Testing" },
            }.ToImmutableDictionary(),
        };

        // Act
        var displayNames = await scopeStore.GetDisplayNamesAsync(scope, CancellationToken.None);

        // Assert
        Assert.Equal(3, displayNames.Count);
    }

    [Fact]
    public async Task Should_ReturnId_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { Name = Guid.NewGuid().ToString() };
        await scopeStore.CreateAsync(scope, CancellationToken.None);

        // Act
        var id = await scopeStore.GetIdAsync(scope, CancellationToken.None);

        // Assert
        Assert.NotNull(id);
        Assert.Equal(scope.Id, id);
    }

    [Fact]
    public async Task Should_ReturnName_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { Name = Guid.NewGuid().ToString() };

        // Act
        var name = await scopeStore.GetNameAsync(scope, CancellationToken.None);

        // Assert
        Assert.Equal(scope.Name, name);
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_PropertiesIsEmpty()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        var properties = await scopeStore.GetPropertiesAsync(scope, CancellationToken.None);

        // Assert
        Assert.Empty(properties);
    }

    [Fact]
    public async Task Should_ReturnNonEmptyDictionary_When_PropertiesExists()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();
        scope.Properties["Test"] = true;
        scope.Properties["Testing"] = "value";
        scope.Properties["Testicles"] = 42;

        // Act
        var properties = await scopeStore.GetPropertiesAsync(scope, CancellationToken.None);

        // Assert
        Assert.Equal(3, properties.Count);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ScopeHasNoResources()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        var resources = await scopeStore.GetResourcesAsync(scope, CancellationToken.None);

        // Assert
        Assert.Empty(resources);
    }

    [Fact]
    public async Task Should_ReturnResources_When_ScopeHasResources()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope
        {
            Resources = ["Thing", "Other-Thing", "More-Things"],
        };

        // Act
        var resources = await scopeStore.GetResourcesAsync(scope, CancellationToken.None);

        // Assert
        Assert.Equal(3, resources.Length);
    }

    [Fact]
    public async Task Should_ReturnNewScope_When_CallingInstantiate()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);

        // Act
        var scope = await scopeStore.InstantiateAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OpenIddictRavenDBScope>(scope);
    }

    [Fact]
    public async Task Should_ReturnList_When_ListingScopes()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);

        var scopeCount = 10;
        var scopeIds = new List<string>();
        foreach (var index in Enumerable.Range(0, scopeCount))
        {
            var scope = new OpenIddictRavenDBScope { Name = $"scope-{index}-{Guid.NewGuid()}" };
            await scopeStore.CreateAsync(scope, CancellationToken.None);
            scopeIds.Add(scope.Id!);
        }
        WaitForIndexing(store);

        // Act
        var scopes = scopeStore.ListAsync(default, default, CancellationToken.None);

        // Assert
        var matchedScopes = new List<OpenIddictRavenDBScope>();
        await foreach (var scope in scopes)
            matchedScopes.Add(scope);
        Assert.True(matchedScopes.Count >= scopeCount);
        Assert.False(scopeIds.Except(matchedScopes.Select(x => x.Id!)).Any());
    }

    [Fact]
    public async Task Should_ReturnFirstFive_When_ListingScopesWithCount()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);

        foreach (var index in Enumerable.Range(0, 10))
        {
            await scopeStore.CreateAsync(new OpenIddictRavenDBScope { Name = $"scope-{index}" }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var expectedCount = 5;
        var scopes = scopeStore.ListAsync(expectedCount, default, CancellationToken.None);

        // Assert
        var matchedScopes = new List<OpenIddictRavenDBScope>();
        await foreach (var scope in scopes)
            matchedScopes.Add(scope);
        Assert.Equal(expectedCount, matchedScopes.Count);
    }

    [Fact]
    public async Task Should_ReturnLastFive_When_ListingScopesWithCountAndOffset()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);

        foreach (var index in Enumerable.Range(0, 10))
        {
            await scopeStore.CreateAsync(new OpenIddictRavenDBScope { Name = $"scope-{index}" }, CancellationToken.None);
        }
        WaitForIndexing(store);

        // Act
        var expectedCount = 5;
        var first = scopeStore.ListAsync(expectedCount, default, CancellationToken.None);
        var firstScopes = new List<OpenIddictRavenDBScope>();
        await foreach (var scope in first)
            firstScopes.Add(scope);

        var scopes = scopeStore.ListAsync(expectedCount, expectedCount, CancellationToken.None);

        // Assert
        var matchedScopes = new List<OpenIddictRavenDBScope>();
        await foreach (var scope in scopes)
            matchedScopes.Add(scope);
        Assert.Equal(expectedCount, matchedScopes.Count);
        Assert.Empty(firstScopes.Select(x => x.Id!).Intersect(matchedScopes.Select(x => x.Id!)));
    }

    [Fact]
    public async Task Should_ReturnScopes_When_ListingWithLinqQuery()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var name = Guid.NewGuid().ToString();
        await scopeStore.CreateAsync(new OpenIddictRavenDBScope { Name = name }, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var scopes = scopeStore.ListAsync<object?, OpenIddictRavenDBScope>(
            (query, _) => query.Where(s => s.Name == name),
            null,
            CancellationToken.None);

        // Assert
        var matchedScopes = new List<OpenIddictRavenDBScope>();
        await foreach (var scope in scopes)
            matchedScopes.Add(scope);
        Assert.Single(matchedScopes);
    }

    [Fact]
    public async Task Should_SetDescription_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();
        var description = Guid.NewGuid().ToString();

        // Act
        await scopeStore.SetDescriptionAsync(scope, description, CancellationToken.None);

        // Assert
        Assert.Equal(description, scope.Description);
    }

    [Fact]
    public async Task Should_SetEmptyDescriptions_When_SetEmptyDictionaryAsDescriptions()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        await scopeStore.SetDescriptionsAsync(scope, ImmutableDictionary<CultureInfo, string>.Empty, CancellationToken.None);

        // Assert
        Assert.Empty(scope.Descriptions);
    }

    [Fact]
    public async Task Should_SetDescriptions_When_SettingDescriptions()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();
        var descriptions = new Dictionary<CultureInfo, string>
        {
            { new CultureInfo("sv-SE"), "Testar" },
            { new CultureInfo("es-ES"), "Testado" },
            { new CultureInfo("en-US"), "Testing" },
        };

        // Act
        await scopeStore.SetDescriptionsAsync(scope, descriptions.ToImmutableDictionary(), CancellationToken.None);

        // Assert
        Assert.Equal(3, scope.Descriptions.Count);
    }

    [Fact]
    public async Task Should_SetDisplayName_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();
        var displayName = Guid.NewGuid().ToString();

        // Act
        await scopeStore.SetDisplayNameAsync(scope, displayName, CancellationToken.None);

        // Assert
        Assert.Equal(displayName, scope.DisplayName);
    }

    [Fact]
    public async Task Should_SetEmptyDisplayNames_When_SetEmptyDictionaryAsDisplayNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        await scopeStore.SetDisplayNamesAsync(scope, ImmutableDictionary<CultureInfo, string>.Empty, CancellationToken.None);

        // Assert
        Assert.Empty(scope.DisplayNames);
    }

    [Fact]
    public async Task Should_SetDisplayNames_When_SettingDisplayNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();
        var displayNames = new Dictionary<CultureInfo, string>
        {
            { new CultureInfo("sv-SE"), "Testar" },
            { new CultureInfo("es-ES"), "Testado" },
            { new CultureInfo("en-US"), "Testing" },
        };

        // Act
        await scopeStore.SetDisplayNamesAsync(scope, displayNames.ToImmutableDictionary(), CancellationToken.None);

        // Assert
        Assert.Equal(3, scope.DisplayNames.Count);
    }

    [Fact]
    public async Task Should_SetName_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();
        var name = Guid.NewGuid().ToString();

        // Act
        await scopeStore.SetNameAsync(scope, name, CancellationToken.None);

        // Assert
        Assert.Equal(name, scope.Name);
    }

    [Fact]
    public async Task Should_SetEmptyProperties_When_SetEmptyDictionaryAsProperties()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        await scopeStore.SetPropertiesAsync(scope, ImmutableDictionary<string, JsonElement>.Empty, CancellationToken.None);

        // Assert
        Assert.Empty(scope.Properties);
    }

    [Fact]
    public async Task Should_SetProperties_When_SettingProperties()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();
        var properties = new Dictionary<string, JsonElement>
        {
            { "Test", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            { "Testing", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            { "Testicles", JsonDocument.Parse("{ \"Test\": true }").RootElement },
        };

        // Act
        await scopeStore.SetPropertiesAsync(scope, properties.ToImmutableDictionary(), CancellationToken.None);

        // Assert
        Assert.Equal(3, scope.Properties.Count);
    }

    [Fact]
    public async Task Should_SetEmptyResources_When_SetEmptyArrayAsResources()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        await scopeStore.SetResourcesAsync(scope, ImmutableArray<string>.Empty, CancellationToken.None);

        // Assert
        Assert.Empty(scope.Resources);
    }

    [Fact]
    public async Task Should_SetResources_When_SettingResources()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope();

        // Act
        await scopeStore.SetResourcesAsync(scope, ImmutableArray.Create("Testar", "Testado", "Testing"), CancellationToken.None);

        // Assert
        Assert.Equal(3, scope.Resources.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToUpdateScopeThatIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await scopeStore.UpdateAsync(default!, CancellationToken.None));
    }

    [Fact]
    public async Task Should_UpdateScope_When_ScopeIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope { Name = Guid.NewGuid().ToString() };
        await scopeStore.CreateAsync(scope, CancellationToken.None);

        // Act
        scope.Name = "updated-name";
        await scopeStore.UpdateAsync(scope, CancellationToken.None);

        // Assert
        var loaded = await scopeStore.FindByIdAsync(scope.Id!, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("updated-name", loaded!.Name);
    }

    [Fact]
    public async Task Should_UpdateScopeWithResources_When_ResourcesIsSet()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var scopeStore = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session);
        var scope = new OpenIddictRavenDBScope
        {
            Resources = ["some-resource"],
        };
        await scopeStore.CreateAsync(scope, CancellationToken.None);

        // Act
        scope.Resources = ["some-new-resource"];
        await scopeStore.UpdateAsync(scope, CancellationToken.None);

        // Assert
        var updated = await scopeStore.FindByIdAsync(scope.Id!, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("some-new-resource", updated!.Resources.First());
    }

    [Fact]
    public async Task Should_ThrowException_When_ConcurrencyConflictOnScopeUpdate()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session1 = store.OpenAsyncSession();
        var scopeStore1 = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session1);
        var scope = new OpenIddictRavenDBScope { Name = Guid.NewGuid().ToString() };
        await scopeStore1.CreateAsync(scope, CancellationToken.None);
        var scopeId = scope.Id!;

        using var session2 = store.OpenAsyncSession();
        var scopeStore2 = new OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>(session2);
        var scope2 = await scopeStore2.FindByIdAsync(scopeId, CancellationToken.None);
        scope2!.Name = "session2-update";
        await scopeStore2.UpdateAsync(scope2, CancellationToken.None);

        // Act & Assert — session1's change vector is now stale
        scope.Name = "session1-update";
        await Assert.ThrowsAsync<Raven.Client.Exceptions.ConcurrencyException>(async () =>
            await scopeStore1.UpdateAsync(scope, CancellationToken.None));
    }
}
