using System;
using System.Runtime.CompilerServices;

namespace H.Containers
{
    public static class ProxyFactory
    {
        public static T Create<T>() where T : class
        {
            var type = typeof(T);
            if (!type.IsInterface)
            {
                throw new ArgumentException("Type must be an Interface");
            }

            return Unsafe.As<T>(TypeFactory.CreateInstance(type));
        }
    }
}
