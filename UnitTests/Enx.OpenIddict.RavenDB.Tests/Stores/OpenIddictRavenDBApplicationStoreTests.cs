using Enx.OpenIddict.RavenDB.Models;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Raven.Client.Documents.Session;

namespace Enx.OpenIddict.RavenDB.Tests;

public abstract class OpenIddictRavenDBApplicationStoreTests : RavenBaseTest
{
    protected abstract bool UseStaticIndexes { get; }

    protected OpenIddictRavenDBApplicationStore<OpenIddictRavenDBApplication>
        CreateStore(IAsyncDocumentSession session) =>
        new(session, CreateOptions(UseStaticIndexes));

    [Fact]
    public async Task Should_IncreaseCount_When_CountingApplicationsAfterCreatingOne()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ClientId = Guid.NewGuid().ToString(),
        };
        var beforeCount = await applicationStore.CountAsync(CancellationToken.None);
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var count = await applicationStore.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(beforeCount + 1, count);
    }

    [Fact]
    public async Task Should_ReturnCount_When_CountingApplicationsBasedOnLinq()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var displayName = Guid.NewGuid().ToString();
        var application = new OpenIddictRavenDBApplication { DisplayName = displayName };
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var count = await applicationStore.CountAsync(
            x => x.Where(y => y.DisplayName == displayName),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToCreateApplicationThatIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.CreateAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_CreateApplication_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ClientId = Guid.NewGuid().ToString(),
        };

        // Act
        await applicationStore.CreateAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(application.Id);
        var loaded = await applicationStore.FindByIdAsync(application.Id!, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(application.ClientId, loaded!.ClientId);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToDeleteApplicationThatIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.DeleteAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_DeleteApplication_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ClientId = Guid.NewGuid().ToString(),
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);

        // Act
        await applicationStore.DeleteAsync(application, CancellationToken.None);

        // Assert
        var deletedApplication = await applicationStore.FindByIdAsync(application.Id!, CancellationToken.None);
        Assert.Null(deletedApplication);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationWithoutIdentifier()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.FindByIdAsync((string)null!, CancellationToken.None));
        Assert.Equal("identifier", exception.ParamName);
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationWithIdentifierThatDoesntExist()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act
        var application = await applicationStore.FindByIdAsync("doesnt-exist", CancellationToken.None);

        // Assert
        Assert.Null(application);
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByExistingIdentifier()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ClientId = Guid.NewGuid().ToString(),
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);

        // Act
        var result = await applicationStore.FindByIdAsync(application.Id!, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(application.ClientId, result!.ClientId);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationByClientIdWithoutIdentifier()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.FindByClientIdAsync((string)null!, CancellationToken.None));
        Assert.Equal("identifier", exception.ParamName);
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationByClientIdWithIdentifierThatDoesntExist()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act
        var application = await applicationStore.FindByClientIdAsync("doesnt-exist", CancellationToken.None);

        // Assert
        Assert.Null(application);
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByClientIdByExistingIdentifier()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ClientId = Guid.NewGuid().ToString(),
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var result = await applicationStore.FindByClientIdAsync(application.ClientId!, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(application.ClientId, result!.ClientId);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationByRedirectUriWithoutAddress()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            applicationStore.FindByRedirectUriAsync((string)null!, CancellationToken.None));
        Assert.Equal("address", exception.ParamName);
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationByRedirectUriWithAddressThatDoesntExist()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act
        var applications = applicationStore.FindByRedirectUriAsync("doesnt-exist", CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var application in applications)
        {
            matchedApplications.Add(application);
        }

        Assert.Empty(matchedApplications);
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByRedirectUriByExistingAddress()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var redirectUri = $"http://test.com/test/redirect/{Guid.NewGuid()}";
        var application = new OpenIddictRavenDBApplication
        {
            RedirectUris = [redirectUri],
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var applications = applicationStore.FindByRedirectUriAsync(redirectUri, CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var matchedApplication in applications)
        {
            matchedApplications.Add(matchedApplication);
        }

        Assert.NotEmpty(matchedApplications);
        Assert.Single(matchedApplications);
        Assert.Equal(matchedApplications[0].Id, application.Id);
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByRedirectUriByExistingAddressAmongOthers()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var redirectUri = $"http://test.com/test/redirect/{Guid.NewGuid()}";
        var application = new OpenIddictRavenDBApplication
        {
            RedirectUris =
            [
                "http://test.com/test/redirect1",
                redirectUri,
                "http://test.com/test/redirect2",
            ],
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var applications = applicationStore.FindByRedirectUriAsync(redirectUri, CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var matchedApplication in applications)
        {
            matchedApplications.Add(matchedApplication);
        }

        Assert.NotEmpty(matchedApplications);
        Assert.Single(matchedApplications);
        Assert.Equal(matchedApplications[0].Id, application.Id);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationByPostLogoutRedirectUriWithoutAddress()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            applicationStore.FindByPostLogoutRedirectUriAsync((string)null!, CancellationToken.None));
        Assert.Equal("address", exception.ParamName);
    }

    [Fact]
    public async Task
        Should_NotThrowException_When_TryingToFindApplicationByPostLogoutRedirectUriWithAddressThatDoesntExist()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act
        var applications = applicationStore.FindByPostLogoutRedirectUriAsync("doesnt-exist", CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var application in applications)
        {
            matchedApplications.Add(application);
        }

        Assert.Empty(matchedApplications);
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByPostLogoutRedirectUriByExistingAddress()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var redirectUri = $"http://test.com/test/redirect/{Guid.NewGuid()}";
        var application = new OpenIddictRavenDBApplication
        {
            PostLogoutRedirectUris = [redirectUri],
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var applications = applicationStore.FindByPostLogoutRedirectUriAsync(redirectUri, CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var matchedApplication in applications)
        {
            matchedApplications.Add(matchedApplication);
        }

        Assert.NotEmpty(matchedApplications);
        Assert.Single(matchedApplications);
        Assert.Equal(matchedApplications[0].Id, application.Id);
    }

    [Fact]
    public async Task
        Should_ReturnApplication_When_TryingToFindApplicationByPostLogoutRedirectUriByExistingAddressAmongOthers()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var redirectUri = $"http://test.com/test/postlogout/{Guid.NewGuid()}";
        var application = new OpenIddictRavenDBApplication
        {
            PostLogoutRedirectUris =
            [
                "http://test.com/test/redirect1",
                redirectUri,
                "http://test.com/test/redirect2",
            ],
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var applications = applicationStore.FindByPostLogoutRedirectUriAsync(redirectUri, CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var matchedApplication in applications)
        {
            matchedApplications.Add(matchedApplication);
        }

        Assert.NotEmpty(matchedApplications);
        Assert.Single(matchedApplications);
        Assert.Equal(matchedApplications[0].Id, application.Id);
    }

    [Fact]
    public async Task Should_ThrowException_When_ToUpdateApplicationThatIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.UpdateAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ThrowConcurrencyException_When_ConcurrentUpdateOccurs()
    {
        // Arrange
        using var store = GetDocumentStore();

        using var session1 = store.OpenAsyncSession();
        var applicationStore1 = CreateStore(session1);
        var application = new OpenIddictRavenDBApplication();
        await applicationStore1.CreateAsync(application, CancellationToken.None);

        using var session2 = store.OpenAsyncSession();
        var applicationStore2 = CreateStore(session2);
        var appInSession2 = await applicationStore2.FindByIdAsync(application.Id!, CancellationToken.None);
        appInSession2!.DisplayName = "Modified by session 2";
        await applicationStore2.UpdateAsync(appInSession2, CancellationToken.None);

        // Act & Assert
        application.DisplayName = "Modified by session 1";
        await Assert.ThrowsAsync<Raven.Client.Exceptions.ConcurrencyException>(async () =>
            await applicationStore1.UpdateAsync(application, CancellationToken.None));
    }

    [Fact]
    public async Task Should_UpdateApplication_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();
        await applicationStore.CreateAsync(application, CancellationToken.None);

        // Act
        application.DisplayName = "Testing To Update";
        await applicationStore.UpdateAsync(application, CancellationToken.None);

        // Assert
        var updatedApplication = await applicationStore.FindByIdAsync(application.Id!, CancellationToken.None);
        Assert.NotNull(updatedApplication);
        Assert.Equal(application.DisplayName, updatedApplication!.DisplayName);
    }

    [Fact]
    public async Task Should_UpdateApplicationWithRedirectUris_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            RedirectUris = ["http://test.com/return"],
            PostLogoutRedirectUris = ["http://test.com/return"],
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);

        // Act
        var redirectUri = "http://test.com/return/again";
        application.RedirectUris = [redirectUri];
        await applicationStore.UpdateAsync(application, CancellationToken.None);

        // Assert
        var updatedApplication = await applicationStore.FindByIdAsync(application.Id!, CancellationToken.None);
        Assert.NotNull(updatedApplication);
        Assert.Equal(redirectUri, updatedApplication!.RedirectUris!.First());
        Assert.Single(updatedApplication.RedirectUris!);
        Assert.Single(updatedApplication.PostLogoutRedirectUris!);
    }

    [Fact]
    public async Task Should_ReturnApplication_When_GetAsyncIsCalledWithValidQuery()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ClientId = Guid.NewGuid().ToString(),
        };
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var result = await applicationStore.GetAsync<object?, OpenIddictRavenDBApplication>(
            (query, _) => query.Where(a => a.ClientId == application.ClientId),
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(application.ClientId, result!.ClientId);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetClientIdAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetClientIdAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnClientId_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ClientId = Guid.NewGuid().ToString(),
        };

        // Act
        var clientId = await applicationStore.GetClientIdAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(clientId);
        Assert.Equal(application.ClientId, clientId);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetClientSecretAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetClientSecretAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnClientSecret_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ClientSecret = Guid.NewGuid().ToString(),
        };

        // Act
        var clientSecret = await applicationStore.GetClientSecretAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(clientSecret);
        Assert.Equal(application.ClientSecret, clientSecret);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetClientTypeAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetClientTypeAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnClientType_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            Type = Guid.NewGuid().ToString(),
        };

        // Act
        var clientType = await applicationStore.GetClientTypeAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(clientType);
        Assert.Equal(application.Type, clientType);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetConsentTypeAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetConsentTypeAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnConsentType_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ConsentType = Guid.NewGuid().ToString(),
        };

        // Act
        var consentType = await applicationStore.GetConsentTypeAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(consentType);
        Assert.Equal(application.ConsentType, consentType);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetDisplayNameAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetDisplayNameAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnDisplayName_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            DisplayName = Guid.NewGuid().ToString(),
        };

        // Act
        var displayName = await applicationStore.GetDisplayNameAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(displayName);
        Assert.Equal(application.DisplayName, displayName);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetIdAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetIdAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnId_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();
        await applicationStore.CreateAsync(application, CancellationToken.None);

        // Act
        var id = await applicationStore.GetIdAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(id);
        Assert.Equal(application.Id, id);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetClientIdAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetClientIdAsync(default!, default, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetClientId_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var clientId = Guid.NewGuid().ToString();
        await applicationStore.SetClientIdAsync(application, clientId, CancellationToken.None);

        // Assert
        Assert.Equal(clientId, application.ClientId);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetClientSecretAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetClientSecretAsync(default!, default, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetClientSecret_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var clientSecret = Guid.NewGuid().ToString();
        await applicationStore.SetClientSecretAsync(application, clientSecret, CancellationToken.None);

        // Assert
        Assert.Equal(clientSecret, application.ClientSecret);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetClientTypeAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetClientTypeAsync(default!, default, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetClientType_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var clientType = Guid.NewGuid().ToString();
        await applicationStore.SetClientTypeAsync(application, clientType, CancellationToken.None);

        // Assert
        Assert.Equal(clientType, application.Type);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetConsentTypeAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetConsentTypeAsync(default!, default, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetConsentType_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var consentType = Guid.NewGuid().ToString();
        await applicationStore.SetConsentTypeAsync(application, consentType, CancellationToken.None);

        // Assert
        Assert.Equal(consentType, application.ConsentType);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetDisplayNameAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetDisplayNameAsync(default!, default, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetDisplayName_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var displayName = Guid.NewGuid().ToString();
        await applicationStore.SetDisplayNameAsync(application, displayName, CancellationToken.None);

        // Assert
        Assert.Equal(displayName, application.DisplayName);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPermissionsAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetPermissionsAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyPermissions()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var permissions = await applicationStore.GetPermissionsAsync(application, CancellationToken.None);

        // Assert
        Assert.Empty(permissions);
    }

    [Fact]
    public async Task Should_ReturnPermissions_When_ApplicationHasPermissions()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            Permissions = ["Get", "Set", "And Other Things"],
        };

        // Act
        var permissions = await applicationStore.GetPermissionsAsync(application, CancellationToken.None);

        // Assert
        Assert.Equal(3, permissions.Length);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPostLogoutRedirectUrisAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetPostLogoutRedirectUrisAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyPostLogoutRedirectUris()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var postLogoutRedirectUris =
            await applicationStore.GetPostLogoutRedirectUrisAsync(application, CancellationToken.None);

        // Assert
        Assert.Empty(postLogoutRedirectUris);
    }

    [Fact]
    public async Task Should_ReturnPostLogoutRedirectUris_When_ApplicationHasPostLogoutRedirectUris()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            PostLogoutRedirectUris =
            [
                "https://test.io/logout",
                "https://test.io/logout/even/more",
                "https://test.io/logout/login/noop/logout",
            ],
        };

        // Act
        var postLogoutRedirectUris =
            await applicationStore.GetPostLogoutRedirectUrisAsync(application, CancellationToken.None);

        // Assert
        Assert.Equal(3, postLogoutRedirectUris.Length);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetRedirectUrisAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetRedirectUrisAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyRedirectUris()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var redirectUris = await applicationStore.GetRedirectUrisAsync(application, CancellationToken.None);

        // Assert
        Assert.Empty(redirectUris);
    }

    [Fact]
    public async Task Should_ReturnRedirectUris_When_ApplicationHasRedirectUris()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            RedirectUris =
            [
                "https://test.io/login",
                "https://test.io/login/even/more",
                "https://test.io/login/logout/noop/login",
            ],
        };

        // Act
        var redirectUris = await applicationStore.GetRedirectUrisAsync(application, CancellationToken.None);

        // Assert
        Assert.Equal(3, redirectUris.Length);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetRequirementsAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetRequirementsAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyRequirements()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var requirements = await applicationStore.GetRequirementsAsync(application, CancellationToken.None);

        // Assert
        Assert.Empty(requirements);
    }

    [Fact]
    public async Task Should_ReturnRequirements_When_ApplicationHasRequirements()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            Requirements = ["Do", "Dont", "Doer"],
        };

        // Act
        var requirements = await applicationStore.GetRequirementsAsync(application, CancellationToken.None);

        // Assert
        Assert.Equal(3, requirements.Length);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetDisplayNamesAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetDisplayNamesAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyDisplayNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var displayNames = await applicationStore.GetDisplayNamesAsync(application, CancellationToken.None);

        // Assert
        Assert.Empty(displayNames);
    }

    [Fact]
    public async Task Should_ReturnDisplayNames_When_ApplicationHasDisplayNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            DisplayNames = new Dictionary<CultureInfo, string>
            {
                { new CultureInfo("sv-SE"), "Testar" },
                { new CultureInfo("es-ES"), "Testado" },
                { new CultureInfo("en-US"), "Testing" },
            }.ToImmutableDictionary(),
        };

        // Act
        var displayNames = await applicationStore.GetDisplayNamesAsync(application, CancellationToken.None);

        // Assert
        Assert.Equal(3, displayNames.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetDisplayNamesAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetDisplayNamesAsync(default!, default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyDictionaryAsDisplayNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        await applicationStore.SetDisplayNamesAsync(
            application,
            ImmutableDictionary.Create<CultureInfo, string>(),
            CancellationToken.None);

        // Assert
        Assert.Empty(application.DisplayNames);
    }

    [Fact]
    public async Task Should_SetDisplayNames_When_SettingDisplayNames()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var displayNames = new Dictionary<CultureInfo, string>
        {
            { new CultureInfo("sv-SE"), "Testar" },
            { new CultureInfo("es-ES"), "Testado" },
            { new CultureInfo("en-US"), "Testing" },
        };
        await applicationStore.SetDisplayNamesAsync(
            application,
            displayNames.ToImmutableDictionary(x => x.Key, x => x.Value),
            CancellationToken.None);

        // Assert
        Assert.NotNull(application.DisplayNames);
        Assert.Equal(3, application.DisplayNames!.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPermissionsAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetPermissionsAsync(default!, default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyListAsPermissions()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        await applicationStore.SetPermissionsAsync(application, default, CancellationToken.None);

        // Assert
        Assert.Empty(application.Permissions);
    }

    [Fact]
    public async Task Should_SetPermissions_When_SettingPermissions()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var permissions = new List<string> { "Get", "Set", "And Other Things" };
        await applicationStore.SetPermissionsAsync(
            application,
            permissions.ToImmutableArray(),
            CancellationToken.None);

        // Assert
        Assert.NotNull(application.Permissions);
        Assert.Equal(3, application.Permissions!.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPostLogoutRedirectUrisAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetPostLogoutRedirectUrisAsync(default!, default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyListAsPostLogoutRedirectUris()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        await applicationStore.SetPostLogoutRedirectUrisAsync(application, default, CancellationToken.None);

        // Assert
        Assert.Empty(application.PostLogoutRedirectUris);
    }

    [Fact]
    public async Task Should_SetPostLogoutRedirectUris_When_SettingPostLogoutRedirectUris()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var postLogoutRedirectUris = new List<string>
        {
            "https://test.io/logout",
            "https://test.io/logout/even/more",
            "https://test.io/logout/login/noop/logout",
        };
        await applicationStore.SetPostLogoutRedirectUrisAsync(
            application,
            postLogoutRedirectUris.ToImmutableArray(),
            CancellationToken.None);

        // Assert
        Assert.NotNull(application.PostLogoutRedirectUris);
        Assert.Equal(3, application.PostLogoutRedirectUris!.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetRedirectUrisAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetRedirectUrisAsync(default!, default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyListAsRedirectUris()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        await applicationStore.SetRedirectUrisAsync(application, default, CancellationToken.None);

        // Assert
        Assert.Empty(application.RedirectUris);
    }

    [Fact]
    public async Task Should_SetRedirectUris_When_SettingRedirectUris()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var redirectUris = new List<string>
        {
            "https://test.io/login",
            "https://test.io/login/even/more",
            "https://test.io/login/logout/noop/login",
        };
        await applicationStore.SetRedirectUrisAsync(
            application,
            redirectUris.ToImmutableArray(),
            CancellationToken.None);

        // Assert
        Assert.NotNull(application.RedirectUris);
        Assert.Equal(3, application.RedirectUris!.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetRequirementsAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetRequirementsAsync(default!, default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyListAsRequirements()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        await applicationStore.SetRequirementsAsync(application, default, CancellationToken.None);

        // Assert
        Assert.Empty(application.Requirements);
    }

    [Fact]
    public async Task Should_SetRequirements_When_SettingRequirements()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var requirements = new List<string> { "Do", "Dont", "Doer" };
        await applicationStore.SetRequirementsAsync(
            application,
            requirements.ToImmutableArray(),
            CancellationToken.None);

        // Assert
        Assert.NotNull(application.Requirements);
        Assert.Equal(3, application.Requirements!.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPropertiesAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetPropertiesAsync(default!, default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyDictionaryAsProperties()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        await applicationStore.SetPropertiesAsync(
            application,
            ImmutableDictionary.Create<string, JsonElement>(),
            CancellationToken.None);

        // Assert
        Assert.Empty(application.Properties);
    }

    [Fact]
    public async Task Should_SetProperties_When_SettingProperties()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var properties = new Dictionary<string, JsonElement>
        {
            { "Test", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            { "Testing", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            { "Testicles", JsonDocument.Parse("{ \"Test\": true }").RootElement },
        };
        await applicationStore.SetPropertiesAsync(
            application,
            properties.ToImmutableDictionary(x => x.Key, x => x.Value),
            CancellationToken.None);

        // Assert
        Assert.NotNull(application.Properties);
        Assert.Equal(3, application.Properties!.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPropertiesAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetPropertiesAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_PropertiesIsEmpty()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var properties = await applicationStore.GetPropertiesAsync(application, CancellationToken.None);

        // Assert
        Assert.Empty(properties);
    }

    [Fact]
    public async Task Should_ReturnNonEmptyDictionary_When_PropertiesExists()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            Properties = new Dictionary<string, object>
            {
                { "Test", new { Something = true } },
                { "Testing", new { Something = true } },
                { "Testicles", new { Something = true } },
            },
        };

        // Act
        var properties = await applicationStore.GetPropertiesAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(properties);
        Assert.Equal(3, properties.Count);
    }

    [Fact]
    public async Task Should_ReturnNewApplication_When_CallingInstantiate()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act
        var application = await applicationStore.InstantiateAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OpenIddictRavenDBApplication>(application);
    }

    [Fact]
    public async Task Should_ReturnApplications_When_ListingWithLinqQuery()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var clientId = Guid.NewGuid().ToString();
        var application = new OpenIddictRavenDBApplication { ClientId = clientId };
        await applicationStore.CreateAsync(application, CancellationToken.None);
        WaitForIndexing(store);

        // Act
        var applications = applicationStore.ListAsync<object?, OpenIddictRavenDBApplication>(
            (query, _) => query.Where(a => a.ClientId == clientId),
            null,
            CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var app in applications)
        {
            matchedApplications.Add(app);
        }

        Assert.Single(matchedApplications);
        Assert.Equal(clientId, matchedApplications[0].ClientId);
    }

    [Fact]
    public async Task Should_ReturnList_When_ListingApplications()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        var applicationCount = 10;
        var applicationIds = new List<string>();
        foreach (var index in Enumerable.Range(0, applicationCount))
        {
            var application = new OpenIddictRavenDBApplication { DisplayName = index.ToString() };
            await applicationStore.CreateAsync(application, CancellationToken.None);
            applicationIds.Add(application.Id!);
        }

        WaitForIndexing(store);

        // Act
        var applications = applicationStore.ListAsync(default, default, CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var application in applications)
        {
            matchedApplications.Add(application);
        }

        Assert.Equal(applicationCount, matchedApplications.Count);
        Assert.False(applicationIds.Except(matchedApplications.Select(x => x.Id!)).Any());
    }

    [Fact]
    public async Task Should_ReturnFirstFive_When_ListingApplicationsWithCount()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        foreach (var index in Enumerable.Range(0, 10))
        {
            await applicationStore.CreateAsync(
                new OpenIddictRavenDBApplication { DisplayName = index.ToString() },
                CancellationToken.None);
        }

        WaitForIndexing(store);

        // Act
        var expectedCount = 5;
        var applications = applicationStore.ListAsync(expectedCount, default, CancellationToken.None);

        // Assert
        var matchedApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var application in applications)
        {
            matchedApplications.Add(application);
        }

        Assert.Equal(expectedCount, matchedApplications.Count);
    }

    [Fact]
    public async Task Should_ReturnLastFive_When_ListingApplicationsWithCountAndOffset()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        foreach (var index in Enumerable.Range(0, 10))
        {
            await applicationStore.CreateAsync(
                new OpenIddictRavenDBApplication { DisplayName = index.ToString() },
                CancellationToken.None);
        }

        WaitForIndexing(store);

        var pageSize = 5;

        // Act
        var firstPage = applicationStore.ListAsync(pageSize, default, CancellationToken.None);
        var firstApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var application in firstPage)
        {
            firstApplications.Add(application);
        }

        var secondPage = applicationStore.ListAsync(pageSize, pageSize, CancellationToken.None);
        var secondApplications = new List<OpenIddictRavenDBApplication>();
        await foreach (var application in secondPage)
        {
            secondApplications.Add(application);
        }

        // Assert
        Assert.Equal(pageSize, secondApplications.Count);
        Assert.Empty(firstApplications.Select(x => x.Id).Intersect(secondApplications.Select(x => x.Id)));
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetApplicationTypeAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetApplicationTypeAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnApplicationType_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
        };

        // Act
        var applicationType = await applicationStore.GetApplicationTypeAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(applicationType);
        Assert.Equal(application.ApplicationType, applicationType);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetApplicationTypeAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetApplicationTypeAsync(default!, default, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetApplicationType_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var applicationType = Guid.NewGuid().ToString();
        await applicationStore.SetApplicationTypeAsync(application, applicationType, CancellationToken.None);

        // Assert
        Assert.Equal(applicationType, application.ApplicationType);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetJsonWebKeySetAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetJsonWebKeySetAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnJsonWebKeySet_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            JsonWebKeySet =
                "{\"e\":\"AQAB\",\"n\":\"nZD7QWmIwj-3N_RZ1qJjX6CdibU87y2l02yMay4KunambalP9g0fU9yZLwLX9WYJINcXZDUf6QeZ-SSbblET-h8Q4OvfSQ7iuu0WqcvBGy8M0qoZ7I-NiChw8dyybMJHgpiP_AyxpCQnp3bQ6829kb3fopbb4cAkOilwVRBYPhRLboXma0cwcllJHPLvMp1oGa7Ad8osmmJhXhM9qdFFASg_OCQdPnYVzp8gOFeOGwlXfSFEgt5vgeU25E-ycUOREcnP7BnMUk7wpwYqlE537LWGOV5z_1Dqcqc9LmN-z4HmNV7b23QZW4_mzKIOY4IqjmnUGgLU9ycFj5YGDCts7Q\",\"alg\":\"RS256\",\"kid\":\"8f796169-0ac4-48a3-a202-fa4f3d814fcd\",\"kty\":\"RSA\",\"use\":\"sig\"}",
        };

        // Act
        var jsonWebKeySet = await applicationStore.GetJsonWebKeySetAsync(application, CancellationToken.None);

        // Assert
        Assert.NotNull(jsonWebKeySet);
    }

    [Fact]
    public async Task Should_ReturnNullResult_When_JsonWebKeySetIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var jsonWebKeySet = await applicationStore.GetJsonWebKeySetAsync(application, CancellationToken.None);

        // Assert
        Assert.Null(jsonWebKeySet);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetJsonWebKeySetAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetJsonWebKeySetAsync(default!, default, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetJsonWebKeySet_When_ApplicationIsValid()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var jsonWebKeySet = JsonWebKeySet.Create(
            "{\"e\":\"AQAB\",\"n\":\"nZD7QWmIwj-3N_RZ1qJjX6CdibU87y2l02yMay4KunambalP9g0fU9yZLwLX9WYJINcXZDUf6QeZ-SSbblET-h8Q4OvfSQ7iuu0WqcvBGy8M0qoZ7I-NiChw8dyybMJHgpiP_AyxpCQnp3bQ6829kb3fopbb4cAkOilwVRBYPhRLboXma0cwcllJHPLvMp1oGa7Ad8osmmJhXhM9qdFFASg_OCQdPnYVzp8gOFeOGwlXfSFEgt5vgeU25E-ycUOREcnP7BnMUk7wpwYqlE537LWGOV5z_1Dqcqc9LmN-z4HmNV7b23QZW4_mzKIOY4IqjmnUGgLU9ycFj5YGDCts7Q\",\"alg\":\"RS256\",\"kid\":\"8f796169-0ac4-48a3-a202-fa4f3d814fcd\",\"kty\":\"RSA\",\"use\":\"sig\"}");
        await applicationStore.SetJsonWebKeySetAsync(application, jsonWebKeySet, CancellationToken.None);

        // Assert
        Assert.NotNull(application.JsonWebKeySet);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetSettingsAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.GetSettingsAsync(default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_ApplicationDoesntHaveAnySettings()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var settings = await applicationStore.GetSettingsAsync(application, CancellationToken.None);

        // Assert
        Assert.Empty(settings);
    }

    [Fact]
    public async Task Should_ReturnSettings_When_ApplicationHasSettings()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication
        {
            Settings = new Dictionary<string, string>
            {
                { "sv-SE", "Testar" },
                { "es-ES", "Testado" },
                { "en-US", "Testing" },
            }.ToImmutableDictionary(),
        };

        // Act
        var settings = await applicationStore.GetSettingsAsync(application, CancellationToken.None);

        // Assert
        Assert.Equal(3, settings.Count);
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetSettingsAndApplicationIsNull()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await applicationStore.SetSettingsAsync(default!, default!, CancellationToken.None));
        Assert.Equal("application", exception.ParamName);
    }

    [Fact]
    public async Task Should_SetEmpty_When_SetEmptyDictionaryAsSettings()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        await applicationStore.SetSettingsAsync(
            application,
            ImmutableDictionary.Create<string, string>(),
            CancellationToken.None);

        // Assert
        Assert.Empty(application.Settings);
    }

    [Fact]
    public async Task Should_SetSettings_When_SettingSettings()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var applicationStore = CreateStore(session);
        var application = new OpenIddictRavenDBApplication();

        // Act
        var settings = new Dictionary<string, string>
        {
            { "Toast", "Testar" },
            { "Toastado", "Testado" },
            { "Toasting", "Testing" },
        };
        await applicationStore.SetSettingsAsync(
            application,
            settings.ToImmutableDictionary(x => x.Key, x => x.Value),
            CancellationToken.None);

        // Assert
        Assert.NotNull(application.Settings);
        Assert.Equal(3, application.Settings!.Count);
    }
}

public class OpenIddictRavenDBApplicationStoreTests_StaticIndexes : OpenIddictRavenDBApplicationStoreTests
{
    protected override bool UseStaticIndexes => true;
}

public class OpenIddictRavenDBApplicationStoreTests_DynamicIndexes : OpenIddictRavenDBApplicationStoreTests
{
    protected override bool UseStaticIndexes => false;
}