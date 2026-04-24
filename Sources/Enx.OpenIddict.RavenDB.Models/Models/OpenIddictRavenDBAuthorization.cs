using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Enx.OpenIddict.RavenDB.Models;

public class OpenIddictRavenDBAuthorization
{
    public string? ApplicationId { get; set; }

    public DateTime? CreationDate { get; set; }

    public string? Id { get; set; }

    public IDictionary<string, object> Properties { get; set; }
        = new Dictionary<string, object>();

    public IReadOnlyList<string> Scopes { get; set; }
        = ImmutableList.Create<string>();

    public List<string> Tokens { get; set; } = [];

    public string? Status { get; set; }

    public string? Subject { get; set; }

    public string? Type { get; set; }
}