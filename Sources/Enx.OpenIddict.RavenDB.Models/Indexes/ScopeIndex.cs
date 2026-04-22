using Enx.OpenIddict.RavenDB.Models;
using Raven.Client.Documents.Indexes;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Enx.OpenIddict.RavenDB.Indexes
{
    public class ScopeIndex : AbstractIndexCreationTask<OpenIddictRavenDBScope, AuthorizationIndex.Result>
    {
        public class Result
        {
            public string? Name { get; set; }
            public virtual IReadOnlyList<string> Resources { get; set; }
                = ImmutableList.Create<string>();
        }

        public ScopeIndex()
        {
            Map = scopes => from scope in scopes
                            select new Result
                            {
                                Name = scope.Name,
                                Resources = scope.Resources
                            };
        }
    }
}
