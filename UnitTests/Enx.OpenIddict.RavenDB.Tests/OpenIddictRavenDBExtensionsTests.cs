using Microsoft.Extensions.DependencyInjection;
using Enx.OpenIddict.RavenDB.Models;

namespace Enx.OpenIddict.RavenDB.Tests;

public class OpenIddictRavenDBExtensionsTests
{
    [Fact]
    public void UseRavenDB_ThrowsAnExceptionForNullBuilder()
    {
        // Arrange
        var builder = (OpenIddictCoreBuilder)null!;

        // Act and assert
        var exception = Assert.Throws<ArgumentNullException>(builder.UseRavenDB);

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void UseRavenDB_RegistersUntypedManagers()
    {
        // Arrange
        var services = new ServiceCollection().AddOptions();
        var builder = new OpenIddictCoreBuilder(services);

        // Act
        builder.UseRavenDB();

        // Assert
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictApplicationManager) &&
            service.ImplementationFactory is not null);
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictAuthorizationManager) &&
            service.ImplementationFactory is not null);
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictScopeManager) &&
            service.ImplementationFactory is not null);
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictTokenManager) &&
            service.ImplementationFactory is not null);
    }

    [Fact]
    public void UseRavenDB_RegistersRavenDBStores()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new OpenIddictCoreBuilder(services);

        // Act
        builder.UseRavenDB();

        // Assert
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictApplicationStore<OpenIddictRavenDBApplication>) &&
            service.ImplementationType == typeof(OpenIddictRavenDBApplicationStore<OpenIddictRavenDBApplication>));
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictAuthorizationStore<OpenIddictRavenDBAuthorization>) &&
            service.ImplementationType == typeof(OpenIddictRavenDBAuthorizationStore<OpenIddictRavenDBAuthorization>));
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictScopeStore<OpenIddictRavenDBScope>) &&
            service.ImplementationType == typeof(OpenIddictRavenDBScopeStore<OpenIddictRavenDBScope>));
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictTokenStore<OpenIddictRavenDBToken>) &&
            service.ImplementationType == typeof(OpenIddictRavenDBTokenStore<OpenIddictRavenDBToken>));
    }
}
