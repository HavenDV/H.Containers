using System;
using System.Collections.Generic;
using System.Reflection;

namespace H.Containers
{
    public static class DirectProxyFactory
    {
        public static Dictionary<object, object> Dictionary { get; } = new Dictionary<object, object>();

        public static event EventHandler<MethodEventArgs>? MethodWillBeCalled;
        public static event EventHandler<MethodEventArgs>? MethodCalled;

        static DirectProxyFactory()
        {
            ProxyFactory.MethodCalled += (sender, args) =>
            {
                if (sender == null ||
                    !Dictionary.TryGetValue(sender, out var obj))
                {
                    return;
                }

                MethodWillBeCalled?.Invoke(sender, args);

                args.ReturnObject = obj.GetType().InvokeMember(args.MethodInfo.Name, BindingFlags.InvokeMethod, null, obj,
                    args.Arguments.ToArray());

                MethodCalled?.Invoke(sender, args);
            };
        }

        public static T CreateInstance<T>(T internalObj) where T : class
        {
            var instance = ProxyFactory.CreateInstance<T>();

            Dictionary.Add(instance, internalObj);

            return instance;
        }

        public static void DeleteInstance(object instance)
        {
            if (Dictionary.ContainsKey(instance))
            {
                Dictionary.Remove(instance);
            }
        }
    }
}
