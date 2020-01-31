using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using H.Utilities.Extensions;

namespace H.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class RemoteProxyServer : IDisposable
    {
        #region Properties

        private IConnection Connection { get; }

        private List<Assembly> Assemblies { get; } = AppDomain.CurrentDomain.GetAssemblies().ToList();
        private Dictionary<string, object> ObjectsDictionary { get; } = new Dictionary<string, object>();

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<Exception>? ExceptionOccurred;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<string>? MessageReceived;

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, exception);
        }

        private void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public RemoteProxyServer(IConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Connection.MessageReceived += async (sender, message) =>
            {
                await OnMessageReceivedAsync(message);
            };
            Connection.ExceptionOccurred += (sender, exception) =>
            {
                OnExceptionOccurred(exception);
            };
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task InitializeAsync(string name, CancellationToken cancellationToken = default)
        {
            await Connection.InitializeAsync(name, cancellationToken);
        }

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            await Connection.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendExceptionAsync(Exception factoryException, CancellationToken cancellationToken = default)
        {
            try
            {
                await Connection.SendMessageAsync($"exception {factoryException.Message} StackTrace: {factoryException.StackTrace}", cancellationToken);
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }
        }

        private async Task OnEventOccurredAsync(
            string hash, string eventName, string connectionName, object?[] args,
            CancellationToken cancellationToken = default)
        {
            await Connection.SendMessageAsync($"raise_event {hash} {eventName} {connectionName}", cancellationToken);

            await Connection.SendAsync(connectionName, args, cancellationToken);
        }

        private async Task OnMessageReceivedAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                message = message ?? throw new ArgumentNullException(nameof(message));

                OnMessageReceived(message);

                var prefix = message.Split(' ').First();
                var postfix = message.Replace(prefix, string.Empty).TrimStart();

                switch (prefix)
                {
                    case "load_assembly":
                        LoadAssembly(postfix);
                        break;

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
                await SendExceptionAsync(exception, cancellationToken);
            }
        }


        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void LoadAssembly(string path)
        {
            var assembly = Assembly.LoadFrom(path);

            Assemblies.Add(assembly);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="postfix"></param>
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
                    try
                    {
                        if (args.ElementAtOrDefault(0) == instance)
                        {
                            args[0] = null;
                        }

                        await OnEventOccurredAsync(hash, name,
                            $"H.Containers.Process_{hash}_{name}_Event_{Guid.NewGuid()}", args);
                    }
                    catch (Exception exception)
                    {
                        await SendExceptionAsync(exception);
                    }
                });
            }

            ObjectsDictionary.Add(hash, instance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="postfix"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunMethodAsync(string postfix, CancellationToken cancellationToken = default)
        {
            var values = postfix.Split(' ');
            var name = values.ElementAtOrDefault(0) ?? throw new InvalidOperationException("Name is null");
            var hash = values.ElementAtOrDefault(1) ?? throw new InvalidOperationException("Hash is null");
            var pipeNamePrefix = values.ElementAtOrDefault(2) ?? throw new InvalidOperationException("PipeNamePrefix is null");

            var instance = ObjectsDictionary[hash];
            var methodInfo = instance.GetType().GetMethod(name)
                             ?? throw new InvalidOperationException($"Method is not found: {name}");

            var args = await Task.WhenAll(methodInfo.GetParameters()
                .Select(async (_, i) => 
                    await Connection.ReceiveAsync<object?>($"{pipeNamePrefix}{i}", cancellationToken)));

            var value = methodInfo.Invoke(instance, args.ToArray());

            await Connection.SendAsync($"{pipeNamePrefix}out", value, cancellationToken);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 
        /// </summary>
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

            Connection.Dispose();
        }

        #endregion
    }
}
