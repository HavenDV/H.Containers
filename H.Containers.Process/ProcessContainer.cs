using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using H.Containers.Extensions;
using H.Pipes;
using H.Pipes.Args;
using H.Utilities;
using H.Utilities.Extensions;

namespace H.Containers
{
    public sealed class ProcessContainer : IContainer, IAsyncDisposable
    {
        #region Properties

        public string Name { get; }
        public bool ForceUpdateApplication { get; set; }

        private System.Diagnostics.Process? Process { get; set; }
        private SingleConnectionPipeClient<string>? PipeClient { get; set; }
        private EmptyProxyFactory ProxyFactory { get; }
        private Dictionary<string, object> HashDictionary { get; } = new Dictionary<string, object>();
        private List<string> LoadedAssemblies { get; } = new List<string>();

        #endregion

        #region Events

        public event EventHandler<Exception>? ExceptionOccurred;

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, exception);
        }

        #endregion

        #region Constructors

        public ProcessContainer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Name = (string.IsNullOrWhiteSpace(name) ? null : "") ?? throw new ArgumentException("Name is empty", nameof(name));

            ProxyFactory = new EmptyProxyFactory();
            ProxyFactory.AsyncMethodCalled += async (sender, args) =>
            {
                if (sender == null)
                {
                    return;
                }

                try
                {
                    args.ReturnObject = await RunMethodAsync(args.MethodInfo, sender, args.Arguments.ToArray(), CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    args.Exception = exception;
                }
            };
        }

        #endregion

        #region Public methods

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (ForceUpdateApplication)
            {
                Application.Clear();
            }

            Application.GetPathAndUnpackIfRequired();

            return Task.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var path = Application.GetPathAndUnpackIfRequired();

            var name = $"{Name}_Pipe";
            Process = System.Diagnostics.Process.Start(path, name);

            PipeClient = new SingleConnectionPipeClient<string>(name);
            PipeClient.MessageReceived += (sender, args) => OnMessageReceived(args.Message);
            PipeClient.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);

            await PipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                message = message ?? throw new ArgumentNullException(nameof(message));

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

        public async Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            path = path ?? throw new ArgumentNullException(nameof(path));
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            await PipeClient.WriteAsync($"load_assembly {path}", cancellationToken).ConfigureAwait(false);

            LoadedAssemblies.Add(path);
        }

        public async Task<T> CreateObjectAsync<T>(string typeName, CancellationToken cancellationToken = default) 
            where T : class
        {
            typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            var instance = ProxyFactory.CreateInstance<T>();
            var hash = GetHash(instance);
            HashDictionary.Add(hash, instance);

            await PipeClient.WriteAsync($"create_object {typeName} {hash}", cancellationToken).ConfigureAwait(false);

            return instance;
        }

        public async Task<T> CreateObjectAsync<T>(Type type, CancellationToken cancellationToken = default)
            where T : class
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            var path = type.Assembly.Location;
            if (!LoadedAssemblies.Contains(path))
            {
                await LoadAssemblyAsync(path, cancellationToken);
            }

            return await CreateObjectAsync<T>(
                type.FullName ?? 
                throw new InvalidOperationException("type.FullName is null"), 
                cancellationToken);
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

        public Task<Type[]> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Type[0]);
        }

        /// <summary>
        /// It will try to wait for the correct completion of the process with the specified timeout.
        /// When canceled with a token, it will kill the process and correctly clear resources.
        /// Default timeout = 1 second
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
        {
            if (Process == null)
            {
                return;
            }
            
            try
            {
                timeout ??= TimeSpan.FromSeconds(1);

                if (PipeClient == null)
                {
                    Process.Kill();
                    return;
                }

                if (!Process.HasExited)
                {
                    await PipeClient.WriteAsync("stop", cancellationToken).ConfigureAwait(false);
                }

                using var cancellationTokenSource = new CancellationTokenSource(timeout.Value);

                while (!Process.HasExited)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationTokenSource.Token).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                }

                Process.Dispose();
                Process = null;
            }
            catch (OperationCanceledException)
            {
                Process?.Kill();
            }
            finally
            {
                PipeClient?.Dispose();
                PipeClient = null;

                Process?.Dispose();
                Process = null;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopAsync().Wait();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }

        #endregion

        #region Private methods

        private static string GetHash(object instance) => $"{instance.GetHashCode()}";

        private async Task<object?> RunMethodAsync(MethodInfo methodInfo, object instance, object?[] args, CancellationToken cancellationToken = default)
        {
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            var hash = GetHash(instance);
            var name = methodInfo.Name;
            var pipeNamePrefix = $"H.Containers.Process_{hash}_{name}_{Guid.NewGuid()}_";
            await PipeClient.WriteAsync($"run_method {name} {hash} {pipeNamePrefix}", cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < args.Length; i++)
            {
                var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await using var client = new SingleConnectionPipeClient<object?>($"{pipeNamePrefix}{i}");

                await client.ConnectAsync(tokenSource.Token);

                await client.WriteAsync(args[i], tokenSource.Token);
            }

            if (methodInfo.ReturnType == typeof(void))
            {
                return null;
            }

            await using var server = new SingleConnectionPipeServer<object?>($"{pipeNamePrefix}out");

            var messageReceivedArgs = await server.WaitEventAsync(
                async token => await server.StartAsync(cancellationToken: token),
                nameof(server.MessageReceived), 
                cancellationToken) as ConnectionMessageEventArgs<object>;

            return messageReceivedArgs?.Message;
        }

        private async Task OnEventAsync(string eventName, string hash, string pipeName, CancellationToken cancellationToken = default)
        {
            await using var server = new SingleConnectionPipeServer<object?[]>(pipeName);

            var messageReceivedArgs = await server.WaitEventAsync(
                async token => await server.StartAsync(cancellationToken: token),
                nameof(server.MessageReceived),
                cancellationToken) as ConnectionMessageEventArgs<object?[]>;
            if (messageReceivedArgs == null)
            {
                throw new InvalidOperationException($"WaitEventAsync for event \"{eventName}\" returns null");
            }

            var args = messageReceivedArgs.Message;
            var instance = HashDictionary[hash];
            instance.RaiseEvent(eventName, args);
        }

        #endregion
    }
}
