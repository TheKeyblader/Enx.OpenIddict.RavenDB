using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Raven.Client.Documents.Session;

namespace Enx.OpenIddict.RavenDB
{
    internal static class OpenIddictRavenDBHelpers
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncDocumentSession session, IQueryable<T> query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (session is null) throw new ArgumentNullException(nameof(session));
            if (query is null) throw new ArgumentNullException(nameof(query));

            var stream = await session.Advanced.StreamAsync(query, cancellationToken);
            try
            {
                while (await stream.MoveNextAsync())
                {
                    yield return stream.Current.Document;
                }
            }
            finally
            {
                if (stream != null)
                    await stream.DisposeAsync();
            }
        }
    }
}
