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

            public virtual IReadOnlyDictionary<CultureInfo, string> DisplayNames { get; set; }
                = ImmutableDictionary.Create<CultureInfo, string>();
        }

        public ScopeIndex()
        {
            Map = scopes => from scope in scopes
                            select new Result
                            {
                                Name = scope.Name,
                                DisplayNames = scope.DisplayNames
                            };
        }
    }
}
