using System;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;

namespace H.Containers.Process
{
    public sealed class ProcessContainer : IDisposable
    {
        #region Properties

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
            PipeClient.MessageReceived += (sender, args) => OnExceptionOccurred(new Exception(args.Message));

            await PipeClient.ConnectAsync(cancellationToken);
        }

        public async Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            await PipeClient.WriteAsync($"load_assembly {path}", cancellationToken);
        }

        public async Task CreateObjectAsync(string typeName, CancellationToken cancellationToken = default)
        {
            await PipeClient.WriteAsync($"create_object {typeName}", cancellationToken);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            PipeClient.Dispose();
            Process?.Dispose();
        }

        #endregion
    }
}
