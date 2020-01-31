using System;
using System.Threading;

namespace H.Utilities.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class CancellationTokenExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="source"></param>
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
