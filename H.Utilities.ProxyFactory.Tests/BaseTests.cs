using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using H.Utilities.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Utilities.Tests
{
    public static class BaseTests
    {
        public static async Task BaseInstanceRemoteTestAsync<T>(string typeName,
            Func<T, CancellationToken, Task> func, 
            CancellationToken cancellationToken)
            where T : class
        {
            var factoryMessagesQueue = new ConcurrentQueue<Message>();
            var serverMessagesQueue = new ConcurrentQueue<Message>();
            var dictionary = new ConcurrentDictionary<string, object?>();

            var factoryConnection = new TestConnection(factoryMessagesQueue, serverMessagesQueue, dictionary);
            var serverConnection = new TestConnection(serverMessagesQueue, factoryMessagesQueue, dictionary);

            var receivedException = (Exception?)null;
            using var cancellationTokenSource = new CancellationTokenSource();
            // ReSharper disable once AccessToDisposedClosure
            await using var registration = cancellationToken.Register(() => cancellationTokenSource.Cancel());

            using var factory = new RemoteProxyFactory(factoryConnection);
            factory.ExceptionOccurred += (sender, exception) =>
            {
                Console.WriteLine($"factory.ExceptionOccurred: {exception}");
                receivedException = exception;

                // ReSharper disable once AccessToDisposedClosure
                cancellationTokenSource.Cancel();
            };
            using var server = new RemoteProxyServer(serverConnection);
            server.ExceptionOccurred += (sender, exception) =>
            {
                Console.WriteLine($"target.ExceptionOccurred: {exception}");
                receivedException = exception;

                // ReSharper disable once AccessToDisposedClosure
                cancellationTokenSource.Cancel();
            };

            await server.InitializeAsync(nameof(RemoteProxyFactoryTests), cancellationTokenSource.Token);
            await factory.InitializeAsync(nameof(RemoteProxyFactoryTests), cancellationTokenSource.Token);

            var instance = await factory.CreateInstanceAsync<T>(typeName, cancellationTokenSource.Token);

            await func(instance, cancellationToken);

            if (receivedException != null)
            {
                Assert.Fail(receivedException.ToString());
            }
        }
    }
}
