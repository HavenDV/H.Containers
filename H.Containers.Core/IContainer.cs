using System;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers
{
    public interface IContainer : IDisposable
    {
        #region Properties

        string Name { get; }

        #endregion

        #region Events

        //event EventHandler<Exception>? ExceptionOccurred;

        #endregion

        #region Public methods

        Task StartAsync(CancellationToken cancellationToken = default);
        Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default);
        Task<Type[]> GetTypesAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        Task CreateObjectAsync(string typeName, CancellationToken cancellationToken = default);

        #endregion
    }
}
