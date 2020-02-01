using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using H.Utilities.Args;
using H.Utilities.Extensions;

namespace H.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class RemoteProxyFactory : IDisposable
    {
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public List<string> LoadedAssemblies { get; } = new List<string>();

        private IConnection Connection { get; }
        private EmptyProxyFactory EmptyProxyFactory { get; } = new EmptyProxyFactory();
        private Dictionary<string, object> HashDictionary { get; } = new Dictionary<string, object>();

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<MethodEventArgs>? MethodCalled;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<MethodEventArgs>? MethodCompleted;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventEventArgs>? EventRaised;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventEventArgs>? EventCompleted;

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

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public RemoteProxyFactory(IConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Connection.MessageReceived += (sender, message) => OnMessageReceived(message);
            Connection.ExceptionOccurred += (sender, exception) => OnExceptionOccurred(exception);

            EmptyProxyFactory.AsyncMethodCalled += async (sender, args) =>
            {
                if (sender == null)
                {
                    return;
                }

                MethodCalled?.Invoke(sender, args);

                if (args.IsCanceled)
                {
                    return;
                }

                try
                {
                    args.ReturnObject = await RunMethodAsync(args.MethodInfo, sender, args.Arguments.ToArray())
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    args.Exception = exception;
                }

                MethodCompleted?.Invoke(sender, args);
            };
            EmptyProxyFactory.EventRaised += (sender, args) => EventRaised?.Invoke(this, args);
            EmptyProxyFactory.EventCompleted += (sender, args) => EventCompleted?.Invoke(this, args);
        }

        #endregion

        #region Public methods

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public async Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            path = path ?? throw new ArgumentNullException(nameof(path));

            await Connection.SendMessageAsync($"load_assembly {path}", cancellationToken).ConfigureAwait(false);

            LoadedAssemblies.Add(path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> CreateInstanceAsync<T>(string typeName, CancellationToken cancellationToken = default)
            where T : class
        {
            typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));

            var instance = EmptyProxyFactory.CreateInstance<T>();
            var hash = GetHash(instance);
            HashDictionary.Add(hash, instance);

            await Connection.SendMessageAsync($"create_object {typeName} {hash}", cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Connection.Dispose();
        }

        #endregion

        #region Private methods

        private static string GetHash(object instance) => $"{instance.GetHashCode()}";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            await Connection.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }

        private async Task<object?> RunMethodAsync(MethodInfo methodInfo, object instance, object?[] args)
        {
            var cancellationToken = args.FirstOrDefault(arg => arg is CancellationToken) as CancellationToken? 
                                    ?? CancellationToken.None;

            var hash = GetHash(instance);
            var name = methodInfo.Name;
            var pipeNamePrefix = $"H.Containers.Process_{hash}_{name}_{Guid.NewGuid()}_";

            await Connection.SendMessageAsync($"run_method {name} {hash} {pipeNamePrefix}", cancellationToken).ConfigureAwait(false);

            cancellationToken.Register(async () =>
            {
                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                try
                {
                    await SendMessageAsync($"cancel_method {name} {hash}", source.Token);
                }
                catch (OperationCanceledException)
                {
                }
            });

            await Task.WhenAll(args
                .Select(async (arg, i) =>
                {
                    if (arg?.GetType() == typeof(CancellationToken))
                    {
                        return;
                    }

                    await Connection.SendAsync($"{pipeNamePrefix}{i}", arg, cancellationToken);
                }));

            var value = await Connection.ReceiveAsync<object?>($"{pipeNamePrefix}out", cancellationToken);
            var type = methodInfo.ReturnType;
            if (type == typeof(Task))
            {
                return Task.CompletedTask;
            }
            if (type.BaseType == typeof(Task))
            {
                var taskType = type.GenericTypeArguments.FirstOrDefault()
                               ?? throw new InvalidOperationException("Task type is null");

                return typeof(Task).GetMethodInfo(nameof(Task.FromResult))
                    .MakeGenericMethod(taskType)
                    .Invoke(null, new[] { value });
            }

            return value;
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                message = message ?? throw new ArgumentNullException(nameof(message));

                MessageReceived?.Invoke(this, message);

                var prefix = message.Split(' ').First();
                var postfix = message.Replace(prefix, string.Empty).TrimStart();

                switch (prefix)
                {
                    case "exception":
                        OnExceptionOccurred(new Exception(postfix));
                        break;

                    case "raise_event":
                        ProcessEventMessage(postfix);
                        break;
                }
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }
        }

        private async void ProcessEventMessage(string message)
        {
            try
            {
                var values = message.Split(' ');
                var hash = values.ElementAtOrDefault(0) ?? throw new InvalidOperationException("Hash is null");
                var eventName = values.ElementAtOrDefault(1) ?? throw new InvalidOperationException("EventName is null");
                var pipeName = values.ElementAtOrDefault(2) ?? throw new InvalidOperationException("PipeName is null");

                await OnEventAsync(eventName, hash, pipeName);
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }
        }

        private async Task OnEventAsync(string eventName, string hash, string pipeName, CancellationToken cancellationToken = default)
        {
            var args = await Connection.ReceiveAsync<object?[]>(pipeName, cancellationToken);
            var instance = HashDictionary[hash];
            instance.RaiseEvent(eventName, args);
        }

        #endregion
    }
}
