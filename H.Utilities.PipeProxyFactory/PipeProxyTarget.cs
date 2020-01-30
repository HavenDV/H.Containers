using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;
using H.Pipes.Extensions;
using H.Utilities.Args;
using H.Utilities.Extensions;

namespace H.Utilities
{
    public sealed class PipeProxyTarget : IDisposable
    {
        #region Properties

        private SingleConnectionPipeServer<string>? PipeServer { get; set; }
        private List<Assembly> Assemblies { get; } = AppDomain.CurrentDomain.GetAssemblies().ToList();
        private Dictionary<string, object> ObjectsDictionary { get; } = new Dictionary<string, object>();

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<Exception>? ExceptionOccurred;

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, exception);
        }

        #endregion

        public async Task InitializeAsync(string name, CancellationToken cancellationToken = default)
        {
            PipeServer = new SingleConnectionPipeServer<string>(name);
            PipeServer.MessageReceived += async (sender, args) =>
            {
                await OnMessageReceivedAsync(args.Message);
            };
            PipeServer.ExceptionOccurred += (sender, args) =>
            {
                OnExceptionOccurred(args.Exception);
            };

            await PipeServer.StartAsync(cancellationToken: cancellationToken);
        }

        private async Task OnExceptionOccurredAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            if (PipeServer == null)
            {
                return;
            }

            await PipeServer.WriteAsync($"exception {exception.Message} StackTrace: {exception.StackTrace}", cancellationToken);
        }

        private async Task OnEventOccurredAsync(PipeEventEventArgs args, CancellationToken cancellationToken = default)
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

        private async Task OnMessageReceivedAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                var prefix = message.Split(' ').First();
                var postfix = message.Replace(prefix, string.Empty).TrimStart();

                switch (prefix)
                {
                    case "create_object":
                        CreateObject(postfix);
                        break;

                    case "run_method":
                        await RunMethodAsync(postfix, cancellationToken);
                        break;
                }
            }
            catch (Exception exception)
            {
                await OnExceptionOccurredAsync(exception, cancellationToken);
            }
        }


        #region Public methods

        public void LoadAssembly(string path)
        {
            var assembly = Assembly.LoadFrom(path);

            Assemblies.Add(assembly);
        }

        public void CreateObject(string postfix)
        {
            ////throw new Exception(string.Join(" ", assembly.GetTypes().Select(i => $"{i.FullName}")));

            var values = postfix.Split(' ');
            var typeName = values.ElementAtOrDefault(0) ?? throw new InvalidOperationException("Name is null");
            var hash = values.ElementAtOrDefault(1) ?? throw new InvalidOperationException("Hash is null");

            var assembly = Assemblies.FirstOrDefault(i =>
                               i.GetTypes().Any(type => type.FullName == typeName))
                           ?? throw new InvalidOperationException($"Assembly with type \"{typeName}\" is not loaded");
            var instance = assembly.CreateInstance(typeName) ?? throw new InvalidOperationException("Instance is null");
            
            foreach (var eventInfo in instance.GetType().GetEvents())
            {
                instance.SubscribeToEvent(eventInfo.Name, async (name, args) =>
                {
                    await OnEventOccurredAsync(new PipeEventEventArgs(hash, name,
                        $"H.Containers.Process_{hash}_{name}_Event_{Guid.NewGuid()}", args));
                });
            }

            ObjectsDictionary.Add(hash, instance);
        }

        public async Task RunMethodAsync(string postfix, CancellationToken cancellationToken = default)
        {
            var values = postfix.Split(' ');
            var name = values.ElementAtOrDefault(0) ?? throw new InvalidOperationException("Name is null");
            var hash = values.ElementAtOrDefault(1) ?? throw new InvalidOperationException("Hash is null");
            var pipeNamePrefix = values.ElementAtOrDefault(2) ?? throw new InvalidOperationException("PipeNamePrefix is null");

            var instance = ObjectsDictionary[hash];
            var methodInfo = instance.GetType().GetMethod(name)
                             ?? throw new InvalidOperationException($"Method is not found: {name}");
            var args = new List<object?>();
            for (var i = 0; i < methodInfo.GetParameters().Length; i++)
            {
                var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await using var server = new SingleConnectionPipeServer<object?>($"{pipeNamePrefix}{i}");

                var messageReceivedArgs = await server.WaitMessageAsync(
                    async token => await server.StartAsync(cancellationToken: token),
                    tokenSource.Token);

                args.Add(messageReceivedArgs.Message);
            }

            var value = methodInfo.Invoke(instance, args.ToArray());
            if (value == null)
            {
                return;
            }

            await using var client = new SingleConnectionPipeClient<object?>($"{pipeNamePrefix}out");

            await client.ConnectAsync(cancellationToken);

            await client.WriteAsync(value, cancellationToken);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            foreach (var pair in ObjectsDictionary)
            {
                var instance = pair.Value;
                if (instance == null)
                {
                    continue;
                }

                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                if (instance is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                }
            }

            ObjectsDictionary.Clear();

            PipeServer?.Dispose();
        }

        #endregion
    }
}
