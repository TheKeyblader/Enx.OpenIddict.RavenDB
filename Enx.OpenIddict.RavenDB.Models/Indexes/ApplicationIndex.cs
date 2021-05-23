using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents.Indexes;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Enx.OpenIddict.RavenDB.Indexes
{
    public class ApplicationIndex : AbstractIndexCreationTask<OpenIddictRavenDBApplication, ApplicationIndex.Result>
    {
        public class Result
        {
            public string? ClientId { get; set; }

            public virtual IReadOnlyList<string> PostLogoutRedirectUris { get; set; }
                = ImmutableList.Create<string>();

            public virtual IReadOnlyList<string> RedirectUris { get; set; }
                = ImmutableList.Create<string>();
        }

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
    }
}
