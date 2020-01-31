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
    public class PipeConnection : IConnection
    {
        #region Properties

        private bool IsFactory { get; }
        private SingleConnectionPipeClient<string>? PipeClient { get; set; }
        private SingleConnectionPipeServer<string>? PipeServer { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<string>? MessageReceived;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<Exception>? ExceptionOccurred;

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, exception);
        }

        private void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isFactory"></param>
        public PipeConnection(bool isFactory)
        {
            IsFactory = isFactory;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task InitializeAsync(string name, CancellationToken cancellationToken = default)
        {
            if (IsFactory)
            {
                PipeClient = new SingleConnectionPipeClient<string>(name);
                PipeClient.MessageReceived += (sender, args) => OnMessageReceived(args.Message);
                PipeClient.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);

                await PipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                PipeServer = new SingleConnectionPipeServer<string>(name);
                PipeServer.MessageReceived += (sender, args) => OnMessageReceived(args.Message);
                PipeServer.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);

                await PipeServer.StartAsync(cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // ReSharper disable once AccessToDisposedClosure
            cancellationToken.Register(() => tokenSource.Cancel());

            if (IsFactory)
            {
                PipeClient = PipeClient ?? throw new InvalidOperationException("PipeClient is null");

                await PipeClient.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                PipeServer = PipeServer ?? throw new InvalidOperationException("PipeServer is null");

                await PipeServer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendAsync<T>(string name, T value, CancellationToken cancellationToken = default)
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
        public async Task<T> ReceiveAsync<T>(string name, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            PipeClient?.Dispose();
            PipeServer?.Dispose();
        }

        #endregion
    }
}
