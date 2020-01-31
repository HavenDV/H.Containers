using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;
using H.Utilities;

namespace H.Containers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ProcessContainer : IContainer, IAsyncDisposable
    {
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public bool ForceUpdateApplication { get; set; }

        private System.Diagnostics.Process? Process { get; set; }
        private SingleConnectionPipeClient<string>? PipeClient { get; set; }
        private PipeProxyFactory ProxyFactory { get; } = new PipeProxyFactory();
        private List<string> LoadedAssemblies { get; } = new List<string>();

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

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public ProcessContainer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Name = (string.IsNullOrWhiteSpace(name) ? null : "") ?? throw new ArgumentException("Name is empty", nameof(name));
        }

        #endregion

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (ForceUpdateApplication)
            {
                Application.Clear();
            }

            Application.GetPathAndUnpackIfRequired();

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var path = Application.GetPathAndUnpackIfRequired();

            var name = $"{Name}_Pipe";

            await ProxyFactory.InitializeAsync($"{name}_ProxyFactoryPipe", cancellationToken);
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
                }
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            path = path ?? throw new ArgumentNullException(nameof(path));
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            await PipeClient.WriteAsync($"load_assembly {path}", cancellationToken).ConfigureAwait(false);

            LoadedAssemblies.Add(path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> CreateObjectAsync<T>(string typeName, CancellationToken cancellationToken = default) 
            where T : class
        {
            typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));

            return await ProxyFactory.CreateInstanceAsync<T>(typeName, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            StopAsync().Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }

        #endregion
    }
}
