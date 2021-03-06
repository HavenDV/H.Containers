﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using H.IO;
using H.Utilities;

namespace H.Containers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ProcessContainer : IContainer, IAsyncDisposable
    {
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Only for methods without CancellationToken argument
        /// </summary>
        public CancellationToken MethodsCancellationToken { get; set; } = CancellationToken.None;

        /// <summary>
        /// For debug purposes
        /// </summary>
        public bool LaunchInCurrentProcess { get; set; }

        /// <summary>
        /// Default -> <see cref="ProcessRuntime.Net50"/>
        /// </summary>
        public ProcessRuntime ProcessRuntime { get; set; } = ProcessRuntime.Net50;

        private TempDirectory ApplicationDirectory { get; } = new ();
        private string? ApplicationPath { get; set; }
        private Process? Process { get; set; }
        private PipeProxyFactory ProxyFactory { get; } = new ();

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
        public ProcessContainer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Name = (string.IsNullOrWhiteSpace(name) ? null : name) ?? throw new ArgumentException("Name is empty", nameof(name));

            ProxyFactory.ExceptionOccurred += (_, exception) => OnExceptionOccurred(exception);
            ProxyFactory.MethodCalled += (_, args) => args.CancellationToken = MethodsCancellationToken;
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
            ApplicationPath = ApplicationDirectory.Unpack(ProcessRuntime);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var name = $"{Name}_Pipe";

            if (LaunchInCurrentProcess)
            {
                var _ = ChildProgram.Main(new[] {name}, false);
            }
            else
            {
                Process = Process.Start(new ProcessStartInfo(ApplicationPath, name));
            }

            await ProxyFactory.InitializeAsync(name, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public async Task LoadAssemblyAsync(string path, CancellationToken cancellationToken = default)
        {
            await ProxyFactory.LoadAssemblyAsync(path, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> CreateObjectAsync<T>(string typeName, CancellationToken cancellationToken = default) 
            where T : class
        {
            typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));

            return await ProxyFactory.CreateInstanceAsync<T>(typeName, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> CreateObjectAsync<T>(Type type, CancellationToken cancellationToken = default)
            where T : class
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            var path = type.Assembly.Location;
            if (!ProxyFactory.LoadedAssemblies.Contains(path))
            {
                await LoadAssemblyAsync(path, cancellationToken).ConfigureAwait(false);
            }

            return await CreateObjectAsync<T>(
                type.FullName ?? 
                throw new InvalidOperationException("type.FullName is null"), 
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IList<string>> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            return await ProxyFactory.GetTypesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// It will try to wait for the correct completion of the process with the specified timeout.
        /// When canceled with a token, it will kill the process and correctly clear resources.
        /// Default timeout = 1 second
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
        {
            timeout ??= TimeSpan.FromSeconds(1);
            if (LaunchInCurrentProcess)
            {
                await ProxyFactory.SendMessageAsync("stop", cancellationToken).ConfigureAwait(false);
                return;
            }

            if (Process == null)
            {
                return;
            }
            
            try
            {
                if (!Process.HasExited)
                {
                    await ProxyFactory.SendMessageAsync("stop", cancellationToken).ConfigureAwait(false);
                }

                using var cancellationTokenSource = new CancellationTokenSource(timeout.Value);

                while (!Process.HasExited)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationTokenSource.Token).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                Process?.Kill();
            }
            finally
            {
                Process?.Dispose();
                Process = null;

                await ProxyFactory.DisposeAsync().ConfigureAwait(false);

                ApplicationDirectory.Dispose();
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            StopAsync(cancellationToken: MethodsCancellationToken).Wait(MethodsCancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            await StopAsync(cancellationToken: MethodsCancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
