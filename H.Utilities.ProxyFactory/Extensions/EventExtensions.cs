using System;

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

            public void HandleEvent(object sender, object? args)
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
            var method = typeof(SubscribeObject).GetMethodInfo(nameof(SubscribeObject.HandleEvent));
            var eventInfo = instance.GetType().GetEvent(eventName) ?? throw new InvalidOperationException("Event info not found");
            // ReSharper disable once ConstantNullCoalescingCondition
            var eventHandlerType = eventInfo.EventHandlerType ?? throw new InvalidOperationException("Event Handler Type not found");
            var delegateObject = Delegate.CreateDelegate(eventHandlerType, subscribeObject, method, true);

            eventInfo.AddEventHandler(instance, delegateObject);
        }
    }
}
