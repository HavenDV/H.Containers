using System;
using System.Collections.Generic;
using System.Reflection;

namespace H.Containers
{
    public class MethodEventArgs : EventArgs
    {
        public List<object?> Arguments { get; set; }
        public MethodInfo MethodInfo { get; set; }

        public object? ReturnObject { get; set; }

        public MethodEventArgs(List<object?> arguments, MethodInfo methodInfo)
        {
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
        }
    }
}
