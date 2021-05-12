using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    internal static class CancellationTokenHelper
    {
        public static CancellationToken MergeCancellationToken(
            CancellationToken ct,
            TimeSpan timeout)
        {
            var timeoutSource = new CancellationTokenSource(timeout);
            var merged = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutSource.Token);

            return merged.Token;
        }
    }
}