using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace H.Utilities.Tests
{
    internal class TestConnection : IConnection
    {
        #region Properties

        public ConcurrentQueue<string> IncomingMessagesQueue { get; }
        public ConcurrentQueue<string> OutgoingMessagesQueue { get; }
        public ConcurrentDictionary<string, object?> Dictionary { get; }

        private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        #endregion

        #region Events

        public event EventHandler<string>? MessageReceived;

        public event EventHandler<Exception>? ExceptionOccurred;

        private void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        #endregion

        #region Constructors

        public TestConnection(
            ConcurrentQueue<string> incomingMessagesQueue,
            ConcurrentQueue<string> outgoingMessagesQueue,
            ConcurrentDictionary<string, object?> dictionary)
        {
            IncomingMessagesQueue = incomingMessagesQueue;
            OutgoingMessagesQueue = outgoingMessagesQueue;
            Dictionary = dictionary;
        }

        #endregion

        #region Public methods

        public Task InitializeAsync(string name, CancellationToken cancellationToken = default)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (IncomingMessagesQueue.TryDequeue(out var message))
                        {
                            OnMessageReceived(message);
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }, CancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            OutgoingMessagesQueue.Enqueue(message);

            return Task.CompletedTask;
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
            while (!Dictionary.TryAdd(name, value))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
            }
        }

        public async Task<T> ReceiveAsync<T>(string name, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (Dictionary.TryGetValue(name, out var value))
                {
                    return value != null ? (T)value : default;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
            }
        }

        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }

        #endregion
    }
}
