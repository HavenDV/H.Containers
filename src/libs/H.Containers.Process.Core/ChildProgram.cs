using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Containers.Extensions;
using H.Utilities;

namespace H.Containers
{
    public static class ChildProgram
    {
        private static CancellationTokenSource CancellationTokenSource { get; } = new ();
        private static PipeProxyServer ProxyServer { get; } = new ();

        public static async Task Main(string[] arguments, bool isSecondProcess)
        {
            arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
            
            try
            {
                var parent = isSecondProcess
                    ? Process.GetCurrentProcess().GetParent()
                    : Process.GetCurrentProcess();
                if (arguments.Length < 1)
                {
                    return;
                }

                var name = arguments.ElementAt(0);

                ProxyServer.MessageReceived += async (_, message) =>
                {
                    await OnMessageReceivedAsync(message).ConfigureAwait(false);
                };
                ProxyServer.ExceptionOccurred += (_, exception) =>
                {
                    Console.Error.WriteLine($"Server Exception: {exception}");
                };

                await ProxyServer.InitializeAsync(name, CancellationTokenSource.Token).ConfigureAwait(false);

                while (!CancellationTokenSource.IsCancellationRequested && 
                       (parent == null || !parent.HasExited))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1), CancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await ProxyServer.DisposeAsync().ConfigureAwait(false);
                CancellationTokenSource.Dispose();
            }
        }

        private static async Task OnMessageReceivedAsync(string message)
        {
            try
            {
                switch (message)
                {
                    case "stop":
                        CancellationTokenSource.Cancel();
                        break;
                }
            }
            catch (Exception exception)
            {
                await ProxyServer.SendExceptionAsync(exception).ConfigureAwait(false);
            }
        }
    }
}
