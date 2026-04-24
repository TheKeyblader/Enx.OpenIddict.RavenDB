using System;
using System.Collections.Generic;

namespace Enx.OpenIddict.RavenDB.Models;

public class OpenIddictRavenDBToken
{
    public string? ApplicationId { get; set; }

    public string? AuthorizationId { get; set; }

    public DateTime? CreationDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public string? Id { get; set; }

    public string? Payload { get; set; }

    public IDictionary<string, object> Properties { get; set; }
        = new Dictionary<string, object>();

    public DateTime? RedemptionDate { get; set; }

    public string? ReferenceId { get; set; }

    public string? Status { get; set; }

    public string? Subject { get; set; }

    public string? Type { get; set; }
}