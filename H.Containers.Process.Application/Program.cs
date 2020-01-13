using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using H.Pipes;

namespace H.Containers.Process.Application
{
    internal static class Program
    {
        private static bool IsStopped { get; set; }
        private static Container Container { get; } = new Container();

        [MTAThread]
        private static async Task Main(string[] arguments)
        {
            if (arguments.Length < 1)
            {
                return;
            }

            var prefix = arguments.ElementAt(0);

            await using var server = new PipeServer<string>(prefix);
            server.MessageReceived += async (sender, args) =>
            {
                await args.Connection.WriteAsync("hello12");
                OnMessageReceived(args.Message);
            };
            server.ExceptionOccurred += (sender, args) =>
            {
                Trace.WriteLine($"Exception: {args.Exception}");
            };
            await server.StartAsync(false);

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

        private static void OnMessageReceived(string message)
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
    }
}
