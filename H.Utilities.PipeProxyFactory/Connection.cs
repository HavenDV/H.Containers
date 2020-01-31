using System;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;
using H.Pipes.Extensions;

namespace H.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class Connection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task SendAsync<T>(string name, T value, CancellationToken cancellationToken = default)
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // ReSharper disable once AccessToDisposedClosure
            cancellationToken.Register(() => tokenSource.Cancel());

            await using var client = new SingleConnectionPipeClient<object?>(name);

            await client.ConnectAsync(tokenSource.Token);

            await client.WriteAsync(value, tokenSource.Token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T> ReceiveAsync<T>(string name, CancellationToken cancellationToken = default)
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // ReSharper disable once AccessToDisposedClosure
            cancellationToken.Register(() => tokenSource.Cancel());
            
            await using var server = new SingleConnectionPipeServer<T>(name);

            var args = await server.WaitMessageAsync(
                async token => await server.StartAsync(cancellationToken: token),
                tokenSource.Token);

            return args.Message;
        }
    }
}
