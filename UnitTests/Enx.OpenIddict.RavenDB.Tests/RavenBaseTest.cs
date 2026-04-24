using Enx.OpenIddict.RavenDB.Models;
using Enx.OpenIddict.RavenDB.Indexes;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Embedded;
using Raven.TestDriver;

namespace Enx.OpenIddict.RavenDB.Tests;

public class RavenBaseTest : RavenTestDriver
{
    static RavenBaseTest()
    {
        ConfigureServer(new TestServerOptions
        {
            Licensing = new ServerOptions.LicensingOptions
            {
                ThrowOnInvalidOrMissingLicense = false
            }
        });
    }

    protected override void SetupDatabase(IDocumentStore documentStore)
    {
        IndexCreation.CreateIndexes([
            new ApplicationIndex<OpenIddictRavenDBApplication>(),
            new AuthorizationIndex<OpenIddictRavenDBAuthorization>(),
            new ScopeIndex<OpenIddictRavenDBScope>(),
            new TokenIndex<OpenIddictRavenDBToken>()
        ], documentStore);
    }

    protected static IOptionsMonitor<OpenIddictRavenDBOptions> CreateOptions(bool useStaticIndexes) =>
        new TestOptionsMonitor<OpenIddictRavenDBOptions>(new OpenIddictRavenDBOptions
            { UseStaticIndexes = useStaticIndexes });

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}