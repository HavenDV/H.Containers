using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers
{
    public class AppDomainContainer : IContainer
    {
        #region Properties

        public string Name { get; }

        public AppDomain? AppDomain { get; set; }
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

        public AppDomainContainer(string name)
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
            AppDomain = AppDomain.CreateDomain(Name);

            return Task.CompletedTask;
        }

        public Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            AppDomain = AppDomain ?? throw new InvalidOperationException("Container is not started");

            var bytes = File.ReadAllBytes(path);

            Assembly = AppDomain.Load(bytes);

            return Task.CompletedTask;
        }

        public Task<IList<string>> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            Assembly = Assembly ?? throw new InvalidOperationException("Assembly is not loaded");

            var types = Assembly
                .GetTypes()
                .Select(type => type.FullName ?? string.Empty)
                .ToArray();

            return Task.FromResult<IList<string>>(types);
        }

        public Task StopAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
        {
            AppDomain = AppDomain ?? throw new InvalidOperationException("Container is not started");

            AppDomain.Unload(AppDomain);
            AppDomain = null;

            return Task.CompletedTask;
        }

        public Task<T> CreateObjectAsync<T>(string typeName, CancellationToken cancellationToken = default)
            where T : class
        {
            Assembly = Assembly ?? throw new InvalidOperationException("Assembly is not loaded");

            return Task.FromResult((T)Assembly.CreateInstance(typeName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (AppDomain == null)
            {
                return;
            }

            StopAsync().Wait();
        }

        #endregion
    }
}
