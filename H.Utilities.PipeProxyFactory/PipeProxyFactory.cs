﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;
using H.Pipes.Args;
using H.Utilities.Args;
using H.Utilities.Extensions;

namespace H.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PipeProxyFactory
    {
        #region Properties

        private SingleConnectionPipeClient<string>? PipeClient { get; set; }
        private EmptyProxyFactory EmptyProxyFactory { get; } = new EmptyProxyFactory();
        private Dictionary<string, object> HashDictionary { get; } = new Dictionary<string, object>();

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<MethodEventArgs>? MethodCalled;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<MethodEventArgs>? MethodCompleted;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventEventArgs>? EventRaised;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventEventArgs>? EventCompleted;

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
        public PipeProxyFactory()
        {
            EmptyProxyFactory.AsyncMethodCalled += async (sender, args) =>
            {
                if (sender == null)
                {
                    return;
                }

                MethodCalled?.Invoke(sender, args);

                if (args.IsCanceled)
                {
                    return;
                }

                try
                {
                    args.ReturnObject = await RunMethodAsync(args.MethodInfo, sender, args.Arguments.ToArray(), CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    args.Exception = exception;
                }

                MethodCompleted?.Invoke(sender, args);
            };
            EmptyProxyFactory.EventRaised += (sender, args) => EventRaised?.Invoke(this, args);
            EmptyProxyFactory.EventCompleted += (sender, args) => EventCompleted?.Invoke(this, args);
        }

        #endregion

        #region Public methods

        public async Task InitializeAsync(string name, CancellationToken cancellationToken = default)
        {
            PipeClient = new SingleConnectionPipeClient<string>(name);
            PipeClient.MessageReceived += (sender, args) => OnMessageReceived(args.Message);
            PipeClient.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);

            await PipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> CreateInstanceAsync<T>(string typeName, CancellationToken cancellationToken = default)
            where T : class
        {
            typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            var instance = EmptyProxyFactory.CreateInstance<T>();
            var hash = GetHash(instance);
            HashDictionary.Add(hash, instance);

            await PipeClient.WriteAsync($"create_object {typeName} {hash}", cancellationToken).ConfigureAwait(false);

            return instance;
        }

        #endregion

        #region Private methods

        private static string GetHash(object instance) => $"{instance.GetHashCode()}";

        private async Task<object?> RunMethodAsync(MethodInfo methodInfo, object instance, object?[] args, CancellationToken cancellationToken = default)
        {
            PipeClient = PipeClient ?? throw new InvalidOperationException("Container is not started");

            var hash = GetHash(instance);
            var name = methodInfo.Name;
            var pipeNamePrefix = $"H.Containers.Process_{hash}_{name}_{Guid.NewGuid()}_";
            await PipeClient.WriteAsync($"run_method {name} {hash} {pipeNamePrefix}", cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < args.Length; i++)
            {
                var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await using var client = new SingleConnectionPipeClient<object?>($"{pipeNamePrefix}{i}");

                await client.ConnectAsync(tokenSource.Token);

                await client.WriteAsync(args[i], tokenSource.Token);
            }

            if (methodInfo.ReturnType == typeof(void))
            {
                return null;
            }

            await using var server = new SingleConnectionPipeServer<object?>($"{pipeNamePrefix}out");

            var messageReceivedArgs = await server.WaitEventAsync(
                async token => await server.StartAsync(cancellationToken: token),
                nameof(server.MessageReceived),
                cancellationToken) as ConnectionMessageEventArgs<object>;

            return messageReceivedArgs?.Message;
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                message = message ?? throw new ArgumentNullException(nameof(message));

                var prefix = message.Split(' ').First();
                var postfix = message.Replace(prefix, string.Empty).TrimStart();

                switch (prefix)
                {
                    case "exception":
                        OnExceptionOccurred(new Exception(postfix));
                        break;

                    case "raise_event":
                        ProcessEventMessage(postfix);
                        break;
                }
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }
        }

        private async void ProcessEventMessage(string message)
        {
            try
            {
                var values = message.Split(' ');
                var hash = values.ElementAtOrDefault(0) ?? throw new InvalidOperationException("Hash is null");
                var eventName = values.ElementAtOrDefault(1) ?? throw new InvalidOperationException("EventName is null");
                var pipeName = values.ElementAtOrDefault(2) ?? throw new InvalidOperationException("PipeName is null");

                await OnEventAsync(eventName, hash, pipeName);
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }
        }

        private async Task OnEventAsync(string eventName, string hash, string pipeName, CancellationToken cancellationToken = default)
        {
            await using var server = new SingleConnectionPipeServer<object?[]>(pipeName);

            var messageReceivedArgs = await server.WaitEventAsync(
                async token => await server.StartAsync(cancellationToken: token),
                nameof(server.MessageReceived),
                cancellationToken) as ConnectionMessageEventArgs<object?[]>;
            if (messageReceivedArgs == null)
            {
                throw new InvalidOperationException($"WaitEventAsync for event \"{eventName}\" returns null");
            }

            var args = messageReceivedArgs.Message;
            var instance = HashDictionary[hash];
            instance.RaiseEvent(eventName, args);
        }

        #endregion
    }
}