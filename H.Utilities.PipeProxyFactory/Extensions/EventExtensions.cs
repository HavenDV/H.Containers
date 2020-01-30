using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace H.Utilities.Extensions
{
    /// <summary>
    /// Extensions that work with <see langword="event"/> <br/>
    /// <![CDATA[Version: 1.0.0.0]]> <br/>
    /// </summary>
    public static class EventExtensions
    {
        private class SubscribeObject
        {
            public string? Name { get; set; }
            public Action<string, object?[]>? Action { get; set; }

            public void OnEventRaised(List<object?> arguments)
            {
                Name = Name ?? throw new InvalidOperationException("Name is null");

                Action?.Invoke(Name, arguments.ToArray());
            }
        }

        /// <summary>
        /// Subscribes to an event by name and calls the delegate after the event occurs
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        public static void SubscribeToEvent(this object instance, string eventName, Action<string, object?[]> action)
        {
            /*
            var subscribeObject = new SubscribeObject
            {
                Name = eventName,
                Action = action,
            };
            var eventInfo = instance.GetType().GetEvent(eventName)
                            ?? throw new InvalidOperationException("Event info is not found");
            // ReSharper disable once ConstantNullCoalescingCondition
            var handlerType = eventInfo.EventHandlerType
                              ?? throw new InvalidOperationException("Event Handler Type is not found");
            var methodInfo = handlerType.GetMethod("Invoke")
                             ?? throw new InvalidOperationException("Invoke method is not found");
            var parameterTypes = methodInfo
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();
            var onMethod = new DynamicMethod($"On{eventInfo.Name}",
                MethodAttributes.Public |
                MethodAttributes.Static,
                CallingConventions.Standard,
                typeof(void),
                parameterTypes,
                typeof(string).Module,
                false);
            var generator = onMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldstr, eventInfo.Name); // [name]

            var listConstructorInfo = typeof(List<object?>).GetConstructor(Array.Empty<Type>()) ??
                                      throw new InvalidOperationException("Constructor of list is not found");
            generator.Emit(OpCodes.Newobj, listConstructorInfo); // [name, list]

            var index = 1; // First argument is this
            var addMethodInfo = typeof(List<object?>).GetMethod(nameof(List<object?>.Add)) ??
                                throw new InvalidOperationException("List.Add is not found");
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                generator.Emit(OpCodes.Dup); // [name, list, list]

                generator.Emit(OpCodes.Ldarg, index); // [name, list, list, arg_i]
                if (parameterInfo.ParameterType.IsValueType)
                {
                    generator.Emit(OpCodes.Box, parameterInfo.ParameterType); // [name, list, list, boxed_arg_i]
                }

                generator.Emit(OpCodes.Callvirt, addMethodInfo); // [name, list]
                index++;
            }

            generator.EmitCall(OpCodes.Call,
                action.Method, null);

            generator.Emit(OpCodes.Ret);

            //throw new Exception($"{onMethod.GetType()}             {handlerType}");
            var @delegate = onMethod.CreateDelegate(handlerType);

            eventInfo.AddEventHandler(instance, @delegate);*/
        }
    }
}
