using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CurrentDomainContainer : IContainer
    {
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public Assembly? Assembly { get; set; }

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
        public CurrentDomainContainer(string name)
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
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            Assembly = Assembly.LoadFrom(path);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Type[]> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            Assembly = Assembly ?? throw new InvalidOperationException("Assembly is not loaded");

            var types = Assembly.GetTypes()
                .ToArray();

            return Task.FromResult(types);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task CreateObjectAsync(string typeName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
        }

        #endregion
    }
}
