using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers
{
    /// <summary>
    /// 
    /// </summary>
    public class AssemblyLoadContextContainer : IContainer, IAsyncDisposable
    {
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public string Directory { get; set; } = string.Empty;
        
        /// <summary>
        /// 
        /// </summary>
        public Assembly? MainAssembly { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public AssemblyLoadContext? AssemblyLoadContext { get; set; }

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
        public AssemblyLoadContextContainer(string name)
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
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = new AssemblyLoadContext(Name, true);
            AssemblyLoadContext.Resolving += (_, name) => 
                AssemblyLoadContext?.LoadFromAssemblyPath(Path.Combine(Directory, $"{name.Name}.dll"));
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
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started.");

            Directory = Path.GetDirectoryName(path) ?? string.Empty;
            MainAssembly = AssemblyLoadContext.LoadFromAssemblyPath(path);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IList<string>> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started.");
            MainAssembly = MainAssembly ?? throw new InvalidOperationException("Assembly is not loaded.");

            var types = MainAssembly.GetTypes()
                .Select(type => type.FullName ?? string.Empty)
                .ToArray();

            return Task.FromResult<IList<string>>(types);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started.");

            var reference = new WeakReference(AssemblyLoadContext, true);

            AssemblyLoadContext.Unload();
            AssemblyLoadContext = null;

            for (var i = 0; reference.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<T> CreateObjectAsync<T>(string typeName, CancellationToken cancellationToken = default)
            where T : class
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("AssemblyLoadContext is not loaded.");
            MainAssembly = MainAssembly ?? throw new InvalidOperationException("Assembly is not loaded.");

            var obj = MainAssembly.CreateInstance(typeName, true)
                      ?? throw new InvalidOperationException("Object is null");
            
            return Task.FromResult((T)obj);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (AssemblyLoadContext == null)
            {
                return;
            }

            StopAsync().Wait();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            if (AssemblyLoadContext == null)
            {
                return;
            }

            await StopAsync();
        }

        #endregion

        #region Private methods

        private void EnsureIsStarted()
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started.");
        }

        #endregion
    }
}
