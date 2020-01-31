using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Containers.Extensions;
using H.Pipes;
using H.Utilities;

namespace H.Containers
{
    internal static class Program
    {
        private static bool IsStopped { get; set; }
        private static PipeProxyServer ProxyServer { get; } = new PipeProxyServer();
        private static SingleConnectionPipeServer<string>? PipeServer { get; set; }

        private static async Task OnExceptionOccurredAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            if (PipeServer == null)
            {
                return;
            }

            await PipeServer.WriteAsync($"exception {exception.Message} StackTrace: {exception.StackTrace}", cancellationToken);
        }

        [MTAThread]
        private static async Task Main(string[] arguments)
        {
            var parent = Process.GetCurrentProcess().GetParent();
            if (arguments.Length < 1)
            {
                return;
            }

            var name = arguments.ElementAt(0);

            PipeServer = new SingleConnectionPipeServer<string>(name);
            PipeServer.MessageReceived += async (sender, args) =>
            {
                await OnMessageReceivedAsync(args.Message);
            };
            PipeServer.ExceptionOccurred += (sender, args) =>
            {
                Console.Error.WriteLine($"Server Exception: {args.Exception}");
            };

            try
            {
                await PipeServer.StartAsync();
                await ProxyServer.InitializeAsync($"{name}_ProxyFactoryPipe");

                while (!IsStopped && (parent == null || !parent.HasExited))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
                }
            }
            finally
            {
                ProxyServer.Dispose();
                await PipeServer.DisposeAsync();
            }
        }

        private static async Task OnMessageReceivedAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                var prefix = message.Split(' ').First();
                var postfix = message.Replace(prefix, string.Empty).TrimStart();

                switch (prefix)
                {
                    case "stop":
                        IsStopped = true;
                        break;

                    case "load_assembly":
                        ProxyServer.LoadAssembly(postfix);
                        break;

                    case "create_object":
                        ProxyServer.CreateObject(postfix);
                        break;

                    case "run_method":
                        await ProxyServer.RunMethodAsync(postfix, cancellationToken);
                        break;
                }
            }
            catch (Exception exception)
            {
                await OnExceptionOccurredAsync(exception, cancellationToken);
            }
        }
    }
}
