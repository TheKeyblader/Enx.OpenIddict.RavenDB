using System;
using System.Collections.Generic;

namespace Enx.OpenIddict.RavenDB.Models
{
    public class OpenIddictRavenDBToken
    {
        public virtual string? ApplicationId { get; set; }

        public virtual string? AuthorizationId { get; set; }

        public virtual DateTime? CreationDate { get; set; }

        public virtual DateTime? ExpirationDate { get; set; }

        public virtual string? Id { get; set; }

        public virtual string? Payload { get; set; }

        public virtual IDictionary<string, object> Properties { get; set; }
            = new Dictionary<string, object>();

        public virtual DateTime? RedemptionDate { get; set; }

        public virtual string? ReferenceId { get; set; }

        public virtual string? Status { get; set; }

        public virtual string? Subject { get; set; }

        public virtual string? Type { get; set; }
    }
}
