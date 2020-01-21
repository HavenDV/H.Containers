using System;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IContainer : IDisposable
    {
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        #endregion

        #region Events

        //event EventHandler<Exception>? ExceptionOccurred;

        #endregion

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Type[]> GetTypesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StopAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CreateObjectAsync(string typeName, CancellationToken cancellationToken = default);

        #endregion
    }
}
