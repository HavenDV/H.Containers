using System.Threading;
using System.Threading.Tasks;

namespace H.Utilities
{
    /// <summary>
    /// Defines the connection used for RemoteProxyFactory and RemoteProxyServer
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Sends data of a certain type to the other side
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendAsync<T>(string name, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receives data of a certain type from the other side
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> ReceiveAsync<T>(string name, CancellationToken cancellationToken = default);
    }
}
