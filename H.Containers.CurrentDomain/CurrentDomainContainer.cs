using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers
{
    public sealed class CurrentDomainContainer : IContainer
    {
        #region Properties

        public string Name { get; }

        public Assembly? Assembly { get; set; }

        #endregion

        #region Events

        public event EventHandler<Exception>? ExceptionOccurred;

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, exception);
        }

        #endregion

        #region Constructors

        public CurrentDomainContainer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Name = (string.IsNullOrWhiteSpace(name) ? null : "") ?? throw new ArgumentException("Name is empty", nameof(name));
        }

        #endregion

        #region Public methods

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            Assembly = Assembly.LoadFrom(path);

            return Task.CompletedTask;
        }

        public Task<Type[]> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            Assembly = Assembly ?? throw new InvalidOperationException("Assembly is not loaded");

            var types = Assembly.GetTypes()
                .ToArray();

            return Task.FromResult(types);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
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
