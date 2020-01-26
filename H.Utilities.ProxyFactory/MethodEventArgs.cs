using System;
using System.Collections.Generic;
using System.Reflection;

namespace H.Utilities
{
    public class MethodEventArgs : EventArgs
    {
        public List<object?> Arguments { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public EmptyProxyFactory ProxyFactory { get; set; }

        public object? ReturnObject { get; set; }
        public bool IsCanceled { get; set; }

        public MethodEventArgs(List<object?> arguments, MethodInfo methodInfo, EmptyProxyFactory proxyFactory)
        {
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            ProxyFactory = proxyFactory ?? throw new ArgumentNullException(nameof(proxyFactory));
        }
    }
}
