using System;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers
{
    public class AssemblyLoadContextContainer : IContainer
    {
        #region Properties

        public string Name { get; }

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

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = new AssemblyLoadContext(Name, true);

            return Task.CompletedTask;
        }

        public Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started");

            AssemblyLoadContext.LoadFromAssemblyPath(path);

            return Task.CompletedTask;
        }

        public Task<Type[]> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started");

            var types = AssemblyLoadContext
                .Assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .ToArray();

            return Task.FromResult(types);
        }

        public Task StopAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started");

            var containerReference = new WeakReference(AssemblyLoadContext, true);

            AssemblyLoadContext.Unload();
            AssemblyLoadContext = null;

            for (var i = 0; containerReference.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return Task.CompletedTask;
        }

        public Task<T> CreateObjectAsync<T>(string typeName, CancellationToken cancellationToken = default)
            where T : class
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("AssemblyLoadContext is not loaded");

            var obj = AssemblyLoadContext.Assemblies.First().CreateInstance(typeName) as T
                      ?? throw new InvalidOperationException("Object is null");

            return Task.FromResult(obj);
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

        #endregion

        #region Private methods

        private void EnsureIsStarted()
        {
            AssemblyLoadContext = AssemblyLoadContext ?? throw new InvalidOperationException("Container is not started");
        }

        #endregion
    }
}
