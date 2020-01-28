using System;
using System.Collections.Generic;
using System.Linq;
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

        //private Assembly? Assembly { get; set; }
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
            //Assembly = Assembly.LoadFile(path);
        }

        public void CreateObject(string postfix)
        {
            var values = postfix.Split(' ');
            //var typeName = values.ElementAtOrDefault(0) ?? throw new InvalidOperationException("Name is null");
            var hash = values.ElementAtOrDefault(1) ?? throw new InvalidOperationException("Hash is null");

            var instance = new SimpleEventClass();

            foreach (var eventInfo in instance.GetType().GetEvents())
            {
                instance.SubscribeToEvent(eventInfo.Name, (name, obj, args) =>
                {
                    OnEventOccurred(new EventEventArgs(hash, name, 
                        $"H.Containers.Process_{hash}_{name}_Event_{Guid.NewGuid()}", args));
                });
            }
            //Assembly = Assembly ?? throw new InvalidOperationException("Assembly is not loaded");

            //Object = Assembly.CreateInstance(typeName);
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

    public interface ISimpleEventClass
    {
        event EventHandler Event1;

        void RaiseEvent1();
        int Method1(int input);
        string Method2(string input);
    }

    public class SimpleEventClass : ISimpleEventClass
    {
        public event EventHandler? Event1;

        public void RaiseEvent1()
        {
            Event1?.Invoke(this, EventArgs.Empty);
        }

        public int Method1(int input)
        {
            return 321 + input;
        }

        public string Method2(string input)
        {
            return $"Hello, input = {input}";
        }
    }
}
