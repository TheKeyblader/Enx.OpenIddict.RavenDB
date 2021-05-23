using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Enx.OpenIddict.RavenDB.Models
{
    public class OpenIddictRavenDBScope
    {
        public virtual string? Description { get; set; }

        public virtual IReadOnlyDictionary<CultureInfo, string> Descriptions { get; set; }
            = ImmutableDictionary.Create<CultureInfo, string>();

        public virtual string? DisplayName { get; set; }

        public virtual IReadOnlyDictionary<CultureInfo, string> DisplayNames { get; set; }
            = ImmutableDictionary.Create<CultureInfo, string>();

        public virtual string? Id { get; set; }

        public virtual string? Name { get; set; }

        public virtual IDictionary<string, object> Properties { get; set; }
            = new Dictionary<string, object>();

        public virtual IReadOnlyList<string> Resources { get; set; } 
            = ImmutableList.Create<string>();
    }
}
