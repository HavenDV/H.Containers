using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Utilities.Tests
{
    public static class BaseTests
    {
        public static async Task BaseInstanceRemoteTestAsync<T>(string typeName, 
            IConnection factoryConnection,
            IConnection serverConnection,
            Func<T, CancellationToken, Task> func, CancellationToken cancellationToken)
            where T : class
        {
            var receivedException = (Exception?)null;
            using var cancellationTokenSource = new CancellationTokenSource();
            // ReSharper disable once AccessToDisposedClosure
            cancellationToken.Register(() => cancellationTokenSource.Cancel());

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
