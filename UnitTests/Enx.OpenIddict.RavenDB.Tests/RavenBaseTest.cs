using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Embedded;
using Raven.TestDriver;
using System;
using System.Collections.Generic;
using System.Text;

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
        IndexCreation.CreateIndexes(typeof(OpenIddictRavenDBApplication).Assembly, documentStore);
    }
}
