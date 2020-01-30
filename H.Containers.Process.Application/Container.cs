using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using H.Containers.Args;
using H.Containers.Extensions;
using H.Pipes;
using H.Pipes.Args;

namespace H.Containers
{
    public sealed class Container : IDisposable
    {
        #region Properties

        private List<Assembly> Assemblies { get; } = new List<Assembly>();
        private Dictionary<string, object> ObjectsDictionary { get; } = new Dictionary<string, object>();

        #endregion

        #region Events

        public event EventHandler<EventEventArgs>? EventOccurred;

        private void OnEventOccurred(EventEventArgs args)
        {
            EventOccurred?.Invoke(this, args);
        }

        #endregion

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
                instance.SubscribeToEvent(eventInfo.Name, (name, obj, args) =>
                {
                    OnEventOccurred(new EventEventArgs(hash, name, 
                        $"H.Containers.Process_{hash}_{name}_Event_{Guid.NewGuid()}", args));
                });
            }

            ObjectsDictionary.Add(hash, instance);
        }

        public async Task RunMethod(string postfix, CancellationToken cancellationToken = default)
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

                var messageReceivedArgs = await server.WaitEventAsync(
                    async token => await server.StartAsync(cancellationToken: token),
                    nameof(server.MessageReceived),
                    tokenSource.Token) as ConnectionMessageEventArgs<object>;

                args.Add(messageReceivedArgs?.Message);
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
            foreach (var (_, instance) in ObjectsDictionary)
            {
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
        }

        #endregion
    }
}
