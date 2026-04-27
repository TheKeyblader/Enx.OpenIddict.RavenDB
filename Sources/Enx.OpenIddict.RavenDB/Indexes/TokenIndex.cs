using System;
using System.Linq;
using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents.Indexes;

namespace Enx.OpenIddict.RavenDB.Indexes;

public class TokenIndex<TToken> : AbstractIndexCreationTask<TToken, TokenIndex<TToken>.Result>
    where TToken : OpenIddictRavenDBToken
{
    public TokenIndex()
    {
        Map = tokens => from token in tokens
            select new Result
            {
                ApplicationId = token.ApplicationId,
                AuthorizationId = token.AuthorizationId,
                AuthorizationStatus = LoadDocument<OpenIddictRavenDBAuthorization>(token.AuthorizationId).Status,
                CreationDate = token.CreationDate,
                Subject = token.Subject,
                Status = token.Status,
                Type = token.Type,
                ReferenceId = token.ReferenceId
            };
    }

    ///<inheritdoc/>
    public override string IndexName => "OpenIddictTokenIndex";

    public class Result
    {
        public string? ApplicationId { get; set; }

        public string? AuthorizationId { get; set; }

        public string? AuthorizationStatus { get; set; }

        public DateTime? CreationDate { get; set; }

        public string? Subject { get; set; }

        public string? Status { get; set; }

        public string? Type { get; set; }

        public string? ReferenceId { get; set; }
    }
}