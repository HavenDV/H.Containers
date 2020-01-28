using System;
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
    }
}
