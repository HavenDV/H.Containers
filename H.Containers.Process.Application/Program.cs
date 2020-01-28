using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Containers.Args;
using H.Pipes;

namespace H.Containers
{
    internal static class Program
    {
        private static bool IsStopped { get; set; }
        private static Container Container { get; } = new Container();
        private static SingleConnectionPipeServer<string>? PipeServer { get; set; }

        private static async Task OnExceptionOccurredAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            if (PipeServer == null)
            {
                return;
            }

            await PipeServer.WriteAsync($"exception {exception.Message}", cancellationToken);
        }

        private static async Task OnEventOccurredAsync(EventEventArgs args, CancellationToken cancellationToken = default)
        {
            if (PipeServer == null)
            {
                return;
            }

            await PipeServer.WriteAsync($"raise_event {args.Hash} {args.EventName} {args.PipeName}", cancellationToken);

            await using var client = new SingleConnectionPipeClient<object?>(args.PipeName);

            await client.ConnectAsync(cancellationToken);

            await client.WriteAsync(args.Args, cancellationToken);
        }

        [MTAThread]
        private static async Task Main(string[] arguments)
        {
            var parent = Process.GetCurrentProcess().GetParent();
            if (arguments.Length < 1)
            {
                return;
            }

            var prefix = arguments.ElementAt(0);

            PipeServer = new SingleConnectionPipeServer<string>(prefix);
            await using var server = PipeServer;
            server.MessageReceived += async (sender, args) =>
            {
                await OnMessageReceivedAsync(args.Message);
            };
            server.ExceptionOccurred += (sender, args) =>
            {
                Console.Error.WriteLine($"Server Exception: {args.Exception}");
            };
            Container.EventOccurred += async (sender, args) =>
            {
                await OnEventOccurredAsync(args);
            };
            await server.StartAsync();

            try
            {
                while (!IsStopped && (parent == null || !parent.HasExited))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
                }
            }
            finally
            {
                Container.Dispose();
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
                        Container.LoadAssembly(postfix);
                        break;

                    case "create_object":
                        Container.CreateObject(postfix);
                        break;

                    case "run_method":
                        await Container.RunMethod(postfix, cancellationToken);
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
