using System;
using System.Collections.Generic;
using System.Linq;

namespace H.Utilities
{
    public class DirectProxyFactory : IDisposable
    {
        public EmptyProxyFactory EmptyProxyFactory { get; } = new EmptyProxyFactory();
        public Dictionary<object, object> Dictionary { get; } = new Dictionary<object, object>();

        public virtual event EventHandler<MethodEventArgs>? MethodCalled;
        public virtual event EventHandler<MethodEventArgs>? MethodCompleted;

        public DirectProxyFactory()
        {
            EmptyProxyFactory.MethodCalled += (sender, args) =>
            {
                if (sender == null ||
                    !Dictionary.TryGetValue(sender, out var obj))
                {
                    return;
                }

                MethodCalled?.Invoke(sender, args);

                if (args.IsCanceled)
                {
                    return;
                }

                var method = obj.GetType().GetMethod(args.MethodInfo.Name, args.MethodInfo
                    .GetParameters()
                    .Select(i => i.ParameterType)
                    .ToArray())
                    ?? throw new InvalidOperationException($"Method not found: {args.MethodInfo}");

                args.ReturnObject = method.Invoke(obj, args.Arguments.ToArray());

                MethodCompleted?.Invoke(sender, args);
            };
        }

        public T CreateInstance<T>(T internalObj) where T : class
        {
            var instance = EmptyProxyFactory.CreateInstance<T>();

            Dictionary.Add(instance, internalObj);

            return instance;
        }

        public void DeleteInstance(object instance)
        {
            if (Dictionary.ContainsKey(instance))
            {
                Dictionary.Remove(instance);
            }
        }

        public void Dispose()
        {
            EmptyProxyFactory.Dispose();
        }
    }
}
