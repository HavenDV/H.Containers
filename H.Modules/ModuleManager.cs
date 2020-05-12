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
    public class ModuleManager<T> where T : class
    {
        #region Properties

        private string Folder { get; }
        private Func<string, IContainer> ContainerFactoryFunc { get; }


        private Dictionary<string, IContainer> Containers { get; } = new Dictionary<string, IContainer>();
        private Dictionary<string, T> Modules { get; } = new Dictionary<string, T>();

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

        public ModuleManager(string folder, Func<string, IContainer> containerFactoryFunc)
        {
            Folder = folder ?? throw new ArgumentNullException(nameof(folder));
            ContainerFactoryFunc = containerFactoryFunc ?? throw new ArgumentNullException(nameof(containerFactoryFunc));
        }

        #endregion

        #region Methods

        private IContainer CreateContainer(string name)
        {
            return ContainerFactoryFunc(name);
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

        public async Task AddModuleAsync(
            string name,
            string typeName, 
            byte[] bytes, 
            CancellationToken cancellationToken = default)
        {
            var container = CreateContainer(name);
            T? instance = null;
            try
            {
                //container.MethodsCancellationToken = cancellationTokenSource.Token,
                container.ExceptionOccurred += (sender, exception) =>
                {
                    OnExceptionOccurred(exception);
                };

                await container.InitializeAsync(cancellationToken);
                await container.StartAsync(cancellationToken);

                Directory.CreateDirectory(Folder);
                var path = Path.Combine(Folder, $"{name}.zip");
                File.WriteAllBytes(path, bytes);

                ZipFile.ExtractToDirectory(path, Folder);

                await container.LoadAssemblyAsync(Path.Combine(Folder, $"{name}.dll"), cancellationToken);

                instance = await container.CreateObjectAsync<T>(typeName, cancellationToken);

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

        #endregion

        #region Event Handlers



        #endregion
    }
}
