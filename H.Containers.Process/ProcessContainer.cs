using System;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;

namespace H.Containers
{
    public sealed class ProcessContainer : IContainer
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

            var name = "testtstststs";
            Process = System.Diagnostics.Process.Start(path, name);
            PipeClient = new PipeClient<string>(name);
            //PipeClient.MessageReceived += (sender, args) => OnExceptionOccurred(new Exception(args.Message));

            await PipeClient.ConnectAsync(cancellationToken);
        }

        public async Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            await PipeClient.WriteAsync($"load_assembly {path}", cancellationToken);
        }

        public async Task CreateObjectAsync(string typeName, CancellationToken cancellationToken = default)
        {
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            await PipeClient.WriteAsync($"create_object {typeName}", cancellationToken);
        }

        public Task<Type[]> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Type[0]);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Process = Process ?? throw new InvalidOperationException("Container is not started");

            Process.Kill();

            return Task.CompletedTask;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            PipeClient?.Dispose();
            Process?.Dispose();
        }

        #endregion
    }
}
