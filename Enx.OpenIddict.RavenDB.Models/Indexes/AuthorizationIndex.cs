using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents.Indexes;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Enx.OpenIddict.RavenDB.Indexes
{
    public class AuthorizationIndex : AbstractIndexCreationTask<OpenIddictRavenDBAuthorization, AuthorizationIndex.Result>
    {
        public class Result
        {
            public string? ApplicationId { get; set; }

            public string? Subject { get; set; }

            public string? Status { get; set; }

            public string? Type { get; set; }

            public virtual IReadOnlyList<string> Scopes { get; set; }
                = ImmutableList.Create<string>();
        }

        public AuthorizationIndex()
        {
            Map = authorizations => from authorization in authorizations
                                    select new Result
                                    {
                                        ApplicationId = authorization.ApplicationId,
                                        Subject = authorization.Subject,
                                        Status = authorization.Status,
                                        Type = authorization.Type,
                                        Scopes = authorization.Scopes
                                    };
        }
    }
}
