using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers.Extensions
{
    /// <summary>
    /// Extensions that work with <see langword="event"/> <br/>
    /// <![CDATA[Version: 1.0.0.0]]> <br/>
    /// </summary>
    public static class EventExtensions
    {
        private class WaitObject
        {
            public TaskCompletionSource<EventArgs?>? Source { get; set; }

            // ReSharper disable UnusedParameter.Local
            public void HandleEvent(object sender, EventArgs e)
            {
                Source?.TrySetResult(e);
            }
        }

        /// <summary>
        /// Asynchronously expects <see langword="event"/> until they occur or until canceled <br/>
        /// <![CDATA[Version: 1.0.0.0]]> <br/>
        /// <![CDATA[Dependency: WaitObject]]> <br/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="eventName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<EventArgs?> WaitEventAsync(this object value, string eventName, CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<EventArgs?>();
            using var cancellationSource = new CancellationTokenSource();

            cancellationSource.Token.Register(() => taskCompletionSource.TrySetCanceled());
            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled());

            var waitObject = new WaitObject
            {
                Source = taskCompletionSource,
            };
            var method = typeof(WaitObject).GetMethod(nameof(WaitObject.HandleEvent)) ?? throw new InvalidOperationException("Method not found");
            var eventInfo = value.GetType().GetEvent(eventName) ?? throw new InvalidOperationException("Event info not found");
            // ReSharper disable once ConstantNullCoalescingCondition
            var eventHandlerType = eventInfo.EventHandlerType ?? throw new InvalidOperationException("Event Handler Type not found");
            var delegateObject = Delegate.CreateDelegate(eventHandlerType, waitObject, method, true);

            try
            {
                eventInfo.AddEventHandler(value, delegateObject);

                return await taskCompletionSource.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                eventInfo.RemoveEventHandler(value, delegateObject);
            }
        }

        /// <summary>
        /// Asynchronously expects <see langword="event"/> until they occur or until canceled <br/>
        /// <![CDATA[Version: 1.0.0.0]]> <br/>
        /// <![CDATA[Dependency: WaitEventAsync(this object value, string eventName, CancellationToken cancellationToken = default)]]> <br/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="func"></param>
        /// <param name="eventName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<EventArgs?> WaitEventAsync(this object value, Func<CancellationToken, Task> func, string eventName, CancellationToken cancellationToken = default)
        {
            try
            {
                var task = value.WaitEventAsync(eventName, cancellationToken);

                await func(cancellationToken).ConfigureAwait(false);

                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private class SubscribeObject
        {
            public string? Name { get; set; }
            public Action<string, object, object?>? Action { get; set; }

            public void HandleEvent<T>(object sender, T args)
            {
                Name = Name ?? throw new InvalidOperationException("Name is null");

                Action?.Invoke(Name, sender, args);
            }
        }

        /// <summary>
        /// Subscribes to an event by name and calls the delegate after the event occurs
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        public static void SubscribeToEvent(this object instance, string eventName, Action<string, object, object?> action)
        {
            var subscribeObject = new SubscribeObject
            {
                Name = eventName,
                Action = action,
            };
            var baseMethod = typeof(SubscribeObject).GetMethod(nameof(SubscribeObject.HandleEvent)) 
                             ?? throw new InvalidOperationException("Method info not found");
            var eventInfo = instance.GetType().GetEvent(eventName) 
                            ?? throw new InvalidOperationException("Event info not found");
            // ReSharper disable once ConstantNullCoalescingCondition
            var eventHandlerType = eventInfo.EventHandlerType 
                                   ?? throw new InvalidOperationException("Event Handler Type not found");
            var method = baseMethod.MakeGenericMethod(eventHandlerType.GetEventArgsType());
            
            var delegateObject = Delegate.CreateDelegate(eventHandlerType, subscribeObject, method, true);

            eventInfo.AddEventHandler(instance, delegateObject);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handlerType"></param>
        /// <returns></returns>
        public static Type GetEventArgsType(this Type handlerType)
        {
            handlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));

            if (handlerType == typeof(EventHandler))
            {
                return typeof(EventArgs);
            }

            if (handlerType.BaseType == typeof(MulticastDelegate) ||
                handlerType.BaseType == typeof(EventHandler))
            {
                return handlerType.GenericTypeArguments.FirstOrDefault()
                       ?? throw new InvalidOperationException("Handler generic type is null");
            }

            return handlerType;
        }
    }
}
