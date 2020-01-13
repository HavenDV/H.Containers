using System;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers.AppDomain
{
    public class AppDomainContainer : IDisposable
    {
        #region Events

        public event EventHandler<Exception>? ExceptionOccurred;

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, exception);
        }

        #endregion

        #region Public methods

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CreateObjectAsync(string typeName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
        }

        #endregion
    }
}
