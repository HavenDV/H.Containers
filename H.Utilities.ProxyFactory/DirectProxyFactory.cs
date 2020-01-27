using System;
using System.Collections.Generic;
using System.Linq;
using H.Utilities.Args;

namespace H.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class DirectProxyFactory : IDisposable
    {
        private EmptyProxyFactory EmptyProxyFactory { get; } = new EmptyProxyFactory();
        private Dictionary<object, object> Dictionary { get; } = new Dictionary<object, object>();

        /// <summary>
        /// 
        /// </summary>
        public virtual event EventHandler<MethodEventArgs>? MethodCalled;

        /// <summary>
        /// 
        /// </summary>
        public virtual event EventHandler<MethodEventArgs>? MethodCompleted;

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="internalObj"></param>
        /// <returns></returns>
        public T CreateInstance<T>(T internalObj) where T : class
        {
            var instance = EmptyProxyFactory.CreateInstance<T>();

            Dictionary.Add(instance, internalObj);

            return instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        public void DeleteInstance(object instance)
        {
            if (Dictionary.ContainsKey(instance))
            {
                Dictionary.Remove(instance);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            EmptyProxyFactory.Dispose();
        }
    }
}
