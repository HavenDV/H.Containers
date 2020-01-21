using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;

namespace H.Containers
{
    internal static class Program
    {
        private static bool IsStopped { get; set; }
        private static Container Container { get; } = new Container();
        private static PipeServer<string>? PipeServer { get; set; }

        private static async Task OnExceptionOccurredAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            if (PipeServer == null)
            {
                return;
            }

            await PipeServer.WriteAsync(exception.Message, predicate: null, cancellationToken);
        }

        [MTAThread]
        private static async Task Main(string[] arguments)
        {
            if (arguments.Length < 1)
            {
                return;
            }

            var prefix = arguments.ElementAt(0);

            PipeServer = new PipeServer<string>(prefix);
            await using var server = PipeServer;
            server.MessageReceived += async (sender, args) =>
            {
                await OnMessageReceivedAsync(args.Message);
            };
            server.ExceptionOccurred += (sender, args) =>
            {
                Console.Error.WriteLine($"Server Exception: {args.Exception}");
            };
            await server.StartAsync();

            try
            {
                while (!IsStopped)
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
                var postfix = message.Replace(prefix, string.Empty);

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
                }
            }
            catch (Exception exception)
            {
                await OnExceptionOccurredAsync(exception, cancellationToken);
            }
        }
    }
}
