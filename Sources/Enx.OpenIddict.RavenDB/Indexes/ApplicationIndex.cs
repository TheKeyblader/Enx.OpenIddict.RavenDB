using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents.Indexes;

namespace Enx.OpenIddict.RavenDB.Indexes;

public class
    ApplicationIndex<TApplication> : AbstractIndexCreationTask<TApplication, ApplicationIndex<TApplication>.Result>
    where TApplication : OpenIddictRavenDBApplication
{
    public ApplicationIndex()
    {
        Map = applications => from application in applications
            select new Result
            {
                ClientId = application.ClientId,
                PostLogoutRedirectUris = application.PostLogoutRedirectUris,
                RedirectUris = application.RedirectUris
            };
    }

    ///<inheritdoc/>
    public override string IndexName => "OpenIddictApplicationIndex";

    public class Result
    {
        public string? ClientId { get; set; }

        public IReadOnlyList<string> PostLogoutRedirectUris { get; set; }
            = ImmutableList.Create<string>();

        public IReadOnlyList<string> RedirectUris { get; set; }
            = ImmutableList.Create<string>();
    }
}