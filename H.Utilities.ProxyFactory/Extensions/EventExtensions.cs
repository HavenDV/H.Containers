using System;
using System.Linq;

namespace H.Utilities.Extensions
{
    /// <summary>
    /// Extensions that work with <see langword="event"/> <br/>
    /// </summary>
    public static class EventExtensions
    {
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
