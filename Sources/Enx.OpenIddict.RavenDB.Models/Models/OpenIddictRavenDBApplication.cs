using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Enx.OpenIddict.RavenDB.Models;

public class OpenIddictRavenDBApplication
{
    public string? ApplicationType { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? ConsentType { get; set; }

    public string? DisplayName { get; set; }

    public IReadOnlyDictionary<CultureInfo, string> DisplayNames { get; set; }
        = ImmutableDictionary.Create<CultureInfo, string>();

    public string? Id { get; set; }

    public string? JsonWebKeySet { get; set; }

    public IReadOnlyList<string> Permissions { get; set; } = [];

    public IReadOnlyList<string> PostLogoutRedirectUris { get; set; } = [];

    public IDictionary<string, object> Properties { get; set; }
        = new Dictionary<string, object>();

    public IReadOnlyList<string> RedirectUris { get; set; } = [];

    public IReadOnlyList<string> Requirements { get; set; } = [];

    public IReadOnlyDictionary<string, string> Settings { get; set; }
        = ImmutableDictionary.Create<string, string>();

    public string? Type { get; set; }
}