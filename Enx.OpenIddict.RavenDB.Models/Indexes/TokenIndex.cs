﻿using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Enx.OpenIddict.RavenDB.Indexes
{
    public class TokenIndex : AbstractIndexCreationTask<OpenIddictRavenDBToken, TokenIndex.Result>
    {
        public class Result
        {
            public string? ApplicationId { get; set; }

            public string? Subject { get; set; }

            public string? Status { get; set; }

            public string? Type { get; set; }

            public string? ReferenceId { get; set; }
        }

        public TokenIndex()
        {
            Map = tokens => from token in tokens
                            select new Result
                            {
                                ApplicationId = token.ApplicationId,
                                Subject = token.Subject,
                                Status = token.Status,
                                Type = token.Type,
                                ReferenceId = token.ReferenceId
                            };
        }
    }
}
