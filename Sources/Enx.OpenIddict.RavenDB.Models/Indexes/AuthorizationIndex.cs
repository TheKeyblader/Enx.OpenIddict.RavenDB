using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Enx.OpenIddict.RavenDB.Indexes;

public class AuthorizationIndex<TAuthorizationIndex> :
    AbstractIndexCreationTask<TAuthorizationIndex, AuthorizationIndex<TAuthorizationIndex>.Result>
    where TAuthorizationIndex : OpenIddictRavenDBAuthorization
{
    public class Result
    {
        public string? ApplicationId { get; set; }

        public DateTime? CreationDate { get; set; }

        public string? Subject { get; set; }

        public string? Status { get; set; }

        public string? Type { get; set; }

        public IReadOnlyList<string> Scopes { get; set; }
            = ImmutableList.Create<string>();

        public List<string> Tokens { get; set; } = [];
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
                Tokens = authorization.Tokens
            };
    }

    ///<inheritdoc/>
    public override string IndexName => "OpenIddictAuthorizationIndex";
}