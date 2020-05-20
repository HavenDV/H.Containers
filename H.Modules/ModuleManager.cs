using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Containers;

namespace H.Modules
{
    public class ModuleManager<TModule> : IDisposable 
        where TModule : class
    {
        #region Properties

        private string Folder { get; }

        private Dictionary<string, IContainer> Containers { get; } = new Dictionary<string, IContainer>();
        private Dictionary<string, TModule> Modules { get; } = new Dictionary<string, TModule>();

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

        public ModuleManager(string folder)
        {
            Folder = folder ?? throw new ArgumentNullException(nameof(folder));
        }

        #endregion

        #region Methods

        private static TContainer CreateContainer<TContainer>(string name)
            where TContainer : IContainer
        {
            return (TContainer)Activator.CreateInstance(typeof(TContainer), name);
        }

        public void TestInitialize()
        {
            try
            {
                //Application.Clear();
                //Application.GetPathAndUnpackIfRequired();
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public async Task<TModule> AddModuleAsync<TContainer>(
            string name,
            string typeName, 
            byte[] bytes,
            Action<TContainer>? initializeAction = null,
            CancellationToken cancellationToken = default)
            where TContainer : IContainer
        {
            var container = CreateContainer<TContainer>(name);
            TModule? instance = null;
            try
            {
                initializeAction?.Invoke(container);

                //container.MethodsCancellationToken = cancellationTokenSource.Token,
                container.ExceptionOccurred += (sender, exception) =>
                {
                    OnExceptionOccurred(exception);
                };

                await container.InitializeAsync(cancellationToken);
                await container.StartAsync(cancellationToken);
                
                Directory.Delete(Folder, true);
                Directory.CreateDirectory(Folder);
                var path = Path.Combine(Folder, $"{name}.zip");
                File.WriteAllBytes(path, bytes);

                ZipFile.ExtractToDirectory(path, Folder);

                await container.LoadAssemblyAsync(Path.Combine(Folder, $"{name}.dll"), cancellationToken);

                instance = await container.CreateObjectAsync<TModule>(typeName, cancellationToken);

                Containers.Add(name, container);
                Modules.Add(name, instance);
            }
            catch (Exception)
            {
                container.Dispose();
                if (instance is IDisposable instanceDisposable)
                {
                    instanceDisposable.Dispose();
                }
                if (Containers.ContainsKey(name))
                {
                    Containers.Remove(name);
                }
                throw;
            }

            return instance ??
                   throw new InvalidOperationException("Instance is null");
        }

        public async Task<IDictionary<string, IList<string>>> GetTypesAsync(
            CancellationToken cancellationToken = default)
        {
            var values = await Task.WhenAll(
                Containers
                    .Select(async pair => (pair.Key, await pair.Value.GetTypesAsync(cancellationToken))));

            return values.ToDictionary(
                pair => pair.Key, 
                pair => pair.Item2);
        }

        public void Dispose()
        {
            foreach (var pair in Modules)
            {
                if (pair.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                if (pair.Value is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                }
            }
            Modules.Clear();

            foreach (var pair in Containers)
            {
                pair.Value.Dispose();
            }
            Containers.Clear();
        }

        #endregion

        #region Event Handlers



        #endregion
    }
}
