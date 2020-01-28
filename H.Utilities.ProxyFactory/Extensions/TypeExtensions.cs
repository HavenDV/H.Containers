using System;
using System.Linq;
using System.Reflection;

namespace H.Utilities.Extensions
{
    /// <summary>
    /// <see cref="Type"/> extensions
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets MethodInfo or throws exception
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="types"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static MethodInfo GetMethodInfo(this Type type, string name, Type[]? types = null)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            name = name ?? throw new ArgumentNullException(nameof(name));

            var method = types != null
                ? type.GetMethod(name, types)
                : type.GetMethod(name);

            return method ?? throw new ArgumentException($"Method \"{name}\" is not found");
        }

        /// <summary>
        /// Gets FieldInfo or throws exception
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static FieldInfo GetPrivateFieldInfo(this IReflect type, string name)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            name = name ?? throw new ArgumentNullException(nameof(name));

            return type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                   ?? throw new ArgumentException($"Private field \"{name}\" is not found");
        }

        /// <summary>
        /// Gets EventInfo or throws exception
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static EventInfo GetEventInfo(this Type type, string name)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            name = name ?? throw new ArgumentNullException(nameof(name));

            return type.GetEvent(name)
                   ?? throw new ArgumentException($"Event \"{name}\" is not found");
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
            if (handlerType.BaseType == typeof(EventHandler))
            {
                return handlerType.GenericTypeArguments.FirstOrDefault()
                       ?? throw new InvalidOperationException("Handler generic type is null");
            }

            return handlerType;
        }
    }
}
