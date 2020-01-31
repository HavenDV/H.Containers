using System;
using System.Threading;

namespace H.Utilities.Extensions
{
    public static class CancellationTokenExtensions
    {
        public static void RegisterSource(this CancellationToken cancellationToken, CancellationTokenSource source)
        {
            source = source ?? throw new ArgumentNullException(nameof(cancellationToken));

            cancellationToken.Register(() =>
            {
                try
                {
                    source.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (AggregateException)
                {
                }
            });
        }
    }
}
