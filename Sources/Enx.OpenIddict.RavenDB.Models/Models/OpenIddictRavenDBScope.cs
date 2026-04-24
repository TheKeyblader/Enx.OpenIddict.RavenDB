using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Enx.OpenIddict.RavenDB.Models;

public class OpenIddictRavenDBScope
{
    public string? Description { get; set; }

    public IReadOnlyDictionary<CultureInfo, string> Descriptions { get; set; }
        = ImmutableDictionary.Create<CultureInfo, string>();

    public string? DisplayName { get; set; }

    public IReadOnlyDictionary<CultureInfo, string> DisplayNames { get; set; }
        = ImmutableDictionary.Create<CultureInfo, string>();

    public string? Id { get; set; }

    public string? Name { get; set; }

    public IDictionary<string, object> Properties { get; set; }
        = new Dictionary<string, object>();

    public IReadOnlyList<string> Resources { get; set; } 
        = ImmutableList.Create<string>();
}