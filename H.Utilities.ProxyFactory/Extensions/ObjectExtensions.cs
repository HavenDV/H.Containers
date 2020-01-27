﻿using System;

namespace H.Utilities.Extensions
{
    public static class ObjectExtensions
    {
        public static void RaiseEvent(this object obj, string eventName, object args)
        {
            obj = obj ?? throw new ArgumentNullException(nameof(obj));
            eventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            args = args ?? throw new ArgumentNullException(nameof(args));

            var method = obj.GetType().GetMethod($"On{eventName}")
                         ?? throw new ArgumentException($"On{eventName} method is not found");

            method.Invoke(obj, new [] { args });
        }
    }
}
