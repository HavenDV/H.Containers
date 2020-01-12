using System;
using System.Reflection;

namespace H.Containers.Process
{
    public sealed class Container : IDisposable
    {
        #region Properties

        private Assembly? Assembly { get; set; }
        private object? Object { get; set; }

        #endregion

        #region Public methods

        public void LoadAssembly(string path)
        {
            Assembly = Assembly.LoadFile(path);
        }

        public void CreateObject(string typeName)
        {
            Assembly = Assembly ?? throw new InvalidOperationException("Assembly is not loaded");

            Object = Assembly.CreateInstance(typeName);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (Object == null)
            {
                return;
            }

            if (Object is IDisposable disposable)
            {
                disposable.Dispose();
            }
            if (Object is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().Wait();
            }

            Object = null;
        }

        #endregion
    }
}
