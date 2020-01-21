using System;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;

namespace H.Containers
{
    public sealed class ProcessContainer : IContainer, IAsyncDisposable
    {
        #region Properties

        public string Name { get; }
        
        private System.Diagnostics.Process? Process { get; set; }
        private PipeClient<string>? PipeClient { get; set; }

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
        }

        #endregion

        #region Public methods

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Application.Clear();

            return Task.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var path = Application.GetPathAndUnpackIfRequired();

            var name = $"{Name}_Pipe";
            Process = System.Diagnostics.Process.Start(path, name);
            PipeClient = new PipeClient<string>(name);
            PipeClient.MessageReceived += (sender, args) => OnExceptionOccurred(new Exception(args.Message));

            await PipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            await PipeClient.WriteAsync($"load_assembly {path}", cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateObjectAsync(string typeName, CancellationToken cancellationToken = default)
        {
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            await PipeClient.WriteAsync($"create_object {typeName}", cancellationToken).ConfigureAwait(false);
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
    }
}
