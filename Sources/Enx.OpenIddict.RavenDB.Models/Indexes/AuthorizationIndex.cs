using Enx.OpenIddict.RavenDB.Models;

using Raven.Client.Documents.Indexes;

using System;
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

            public DateTime? CreationDate { get; set; }

            public string? Subject { get; set; }

            public string? Status { get; set; }

            public string? Type { get; set; }

            public virtual IReadOnlyList<string> Scopes { get; set; }
                = ImmutableList.Create<string>();

            public virtual IEnumerable<bool> ValidTokens { get; set; }
                = new List<bool>();
        }

        public AuthorizationIndex()
        {
            Map = authorizations => from authorization in authorizations
                                    select new Result
                                    {
                                        ApplicationId = authorization.ApplicationId,
                                        CreationDate = authorization.CreationDate,
                                        Subject = authorization.Subject,
                                        Status = authorization.Status,
                                        Type = authorization.Type,
                                        Scopes = authorization.Scopes,
                                        ValidTokens = LoadDocument<OpenIddictRavenDBToken>(authorization.Tokens)
                                            .Select(t => t != null)
                                    };
        }
    }
}
