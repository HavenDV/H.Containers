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
    public class AssemblyLoadContextContainer : IContainer, IAsyncDisposable
    {
        #region Properties

        public string Name { get; }
        public string Directory { get; set; } = string.Empty;
        public Assembly? MainAssembly { get; set; }

        public AssemblyLoadContext? AssemblyLoadContext { get; set; }

        #endregion

        #region Events

        public event EventHandler<Exception>? ExceptionOccurred;

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, exception);
        }

        #endregion

        #region Constructors

        public AssemblyLoadContextContainer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Name = (string.IsNullOrWhiteSpace(name) ? null : "") ?? throw new ArgumentException("Name is empty", nameof(name));
        }

        #endregion

        #region Public methods

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = new AssemblyLoadContext(Name, true);
            AssemblyLoadContext.Resolving += (_, name) => 
                AssemblyLoadContext?.LoadFromAssemblyPath(Path.Combine(Directory, $"{name.Name}.dll"));
            return Task.CompletedTask;
        }

        public Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started.");

            Directory = Path.GetDirectoryName(path) ?? string.Empty;
            MainAssembly = AssemblyLoadContext.LoadFromAssemblyPath(path);

            return Task.CompletedTask;
        }

        public Task<IList<string>> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started.");
            MainAssembly = MainAssembly ?? throw new InvalidOperationException("Assembly is not loaded.");

            var types = MainAssembly.GetTypes()
                .Select(type => type.FullName ?? string.Empty)
                .ToArray();

            return Task.FromResult<IList<string>>(types);
        }

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

        public void Dispose()
        {
            if (AssemblyLoadContext == null)
            {
                return;
            }

            StopAsync().Wait();
        }
        
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
