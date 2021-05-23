using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Enx.OpenIddict.RavenDB.Models
{
    public class OpenIddictRavenDBAuthorization
    {
        public virtual string? ApplicationId { get; set; }

        public virtual DateTime? CreationDate { get; set; }

        public virtual string? Id { get; set; }

        public virtual IDictionary<string, object> Properties { get; set; }
            = new Dictionary<string, object>();

        public virtual IReadOnlyList<string> Scopes { get; set; }
            = ImmutableList.Create<string>();

        public virtual IDictionary<string, OpenIddictRavenDBToken> Tokens { get; set; }
            = new Dictionary<string, OpenIddictRavenDBToken>();

        public virtual string? Status { get; set; }

        public virtual string? Subject { get; set; }

        public virtual string? Type { get; set; }
    }
}
