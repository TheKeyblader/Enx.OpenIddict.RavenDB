using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Enx.OpenIddict.RavenDB.Models
{
    public class OpenIddictRavenDBApplication
    {
        public virtual string? ApplicationType { get; set; }

        public virtual string? ClientId { get; set; }

        public virtual string? ClientSecret { get; set; }

        public virtual string? ConsentType { get; set; }

        public virtual string? DisplayName { get; set; }

        public virtual IReadOnlyDictionary<CultureInfo, string> DisplayNames { get; set; }
            = ImmutableDictionary.Create<CultureInfo, string>();

        public virtual string? Id { get; set; }

        public virtual string? JsonWebKeySet { get; set; }

        public virtual IReadOnlyList<string> Permissions { get; set; } = [];

        public virtual IReadOnlyList<string> PostLogoutRedirectUris { get; set; } = [];

        public virtual IDictionary<string, object> Properties { get; set; }
            = new Dictionary<string, object>();

        public virtual IReadOnlyList<string> RedirectUris { get; set; } = [];

        public virtual IReadOnlyList<string> Requirements { get; set; } = [];

        public virtual IReadOnlyDictionary<string, string> Settings { get; set; }
            = ImmutableDictionary.Create<string, string>();

        public virtual string? Type { get; set; }


    }
}
