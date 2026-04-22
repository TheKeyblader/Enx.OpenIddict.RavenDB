using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Enx.OpenIddict.RavenDB.Models;
using Xunit;

namespace Enx.OpenIddict.RavenDB.Tests;

public class OpenIddictRavenDBBuilderTests
{
    [Fact]
    public void Constructor_ThrowsAnExceptionForNullServices()
    {
        // Arrange
        var services = (IServiceCollection)null!;

        // Act and assert
        var exception = Assert.Throws<ArgumentNullException>(() => new OpenIddictRavenDBBuilder(services));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void ReplaceDefaultApplicationEntity_StoreIsCorrectlyReplaced()
    {
        // Arrange
        var services = CreateServices();
        var builder = CreateBuilder(services);

        // Act
        builder.ReplaceDefaultApplicationEntity<CustomApplication>();

        // Assert
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictApplicationStore<CustomApplication>) &&
            service.ImplementationType == typeof(OpenIddictRavenDBApplicationStore<CustomApplication>));
    }

    [Fact]
    public void ReplaceDefaultAuthorizationEntity_StoreIsCorrectlyReplaced()
    {
        // Arrange
        var services = CreateServices();
        var builder = CreateBuilder(services);

        // Act
        builder.ReplaceDefaultAuthorizationEntity<CustomAuthorization>();

        // Assert
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictAuthorizationStore<CustomAuthorization>) &&
            service.ImplementationType == typeof(OpenIddictRavenDBAuthorizationStore<CustomAuthorization>));
    }

    [Fact]
    public void ReplaceDefaultScopeEntity_StoreIsCorrectlyReplaced()
    {
        // Arrange
        var services = CreateServices();
        var builder = CreateBuilder(services);

        // Act
        builder.ReplaceDefaultScopeEntity<CustomScope>();

        // Assert
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictScopeStore<CustomScope>) &&
            service.ImplementationType == typeof(OpenIddictRavenDBScopeStore<CustomScope>));
    }

    [Fact]
    public void ReplaceDefaultTokenEntity_StoreIsCorrectlyReplaced()
    {
        // Arrange
        var services = CreateServices();
        var builder = CreateBuilder(services);

        // Act
        builder.ReplaceDefaultTokenEntity<CustomToken>();

        // Assert
        Assert.Contains(services, service =>
            service.Lifetime == ServiceLifetime.Scoped &&
            service.ServiceType == typeof(IOpenIddictTokenStore<CustomToken>) &&
            service.ImplementationType == typeof(OpenIddictRavenDBTokenStore<CustomToken>));
    }

    private static OpenIddictRavenDBBuilder CreateBuilder(IServiceCollection services)
        => services.AddOpenIddict().AddCore().UseRavenDB();

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        return services;
    }

    public class CustomApplication : OpenIddictRavenDBApplication { }
    public class CustomAuthorization : OpenIddictRavenDBAuthorization { }
    public class CustomScope : OpenIddictRavenDBScope { }
    public class CustomToken : OpenIddictRavenDBToken { }
}
