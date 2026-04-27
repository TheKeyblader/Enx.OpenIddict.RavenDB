using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents.Indexes;

namespace Enx.OpenIddict.RavenDB.Indexes;

public class ScopeIndex<TScope> : AbstractIndexCreationTask<TScope, ScopeIndex<TScope>.Result>
    where TScope : OpenIddictRavenDBScope
{
    public ScopeIndex()
    {
        Map = scopes => from scope in scopes
            select new Result
            {
                Name = scope.Name,
                Resources = scope.Resources
            };
    }

    ///<inheritdoc/>
    public override string IndexName => "OpenIddictScopeIndex";

    public class Result
    {
        public string? Name { get; set; }

        public IReadOnlyList<string> Resources { get; set; }
            = ImmutableList.Create<string>();
    }
}