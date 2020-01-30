using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace H.Containers.Extensions
{
    /// <summary>
    /// Extensions that work with <see langword="event"/> <br/>
    /// <![CDATA[Version: 1.0.0.0]]> <br/>
    /// </summary>
    public static class EventExtensions
    {
        private class WaitObject
        {
            public TaskCompletionSource<EventArgs?>? Source { get; set; }

            // ReSharper disable UnusedParameter.Local
            public void HandleEvent(object sender, EventArgs e)
            {
                Source?.TrySetResult(e);
            }
        }

        /// <summary>
        /// Asynchronously expects <see langword="event"/> until they occur or until canceled <br/>
        /// <![CDATA[Version: 1.0.0.0]]> <br/>
        /// <![CDATA[Dependency: WaitObject]]> <br/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="eventName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<EventArgs?> WaitEventAsync(this object value, string eventName, CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<EventArgs?>();
            using var cancellationSource = new CancellationTokenSource();

            cancellationSource.Token.Register(() => taskCompletionSource.TrySetCanceled());
            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled());

            var waitObject = new WaitObject
            {
                Source = taskCompletionSource,
            };
            var method = typeof(WaitObject).GetMethod(nameof(WaitObject.HandleEvent)) ?? throw new InvalidOperationException("Method not found");
            var eventInfo = value.GetType().GetEvent(eventName) ?? throw new InvalidOperationException("Event info not found");
            // ReSharper disable once ConstantNullCoalescingCondition
            var eventHandlerType = eventInfo.EventHandlerType ?? throw new InvalidOperationException("Event Handler Type not found");
            var delegateObject = Delegate.CreateDelegate(eventHandlerType, waitObject, method, true);

            try
            {
                eventInfo.AddEventHandler(value, delegateObject);

                return await taskCompletionSource.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                eventInfo.RemoveEventHandler(value, delegateObject);
            }
        }

        /// <summary>
        /// Asynchronously expects <see langword="event"/> until they occur or until canceled <br/>
        /// <![CDATA[Version: 1.0.0.0]]> <br/>
        /// <![CDATA[Dependency: WaitEventAsync(this object value, string eventName, CancellationToken cancellationToken = default)]]> <br/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="func"></param>
        /// <param name="eventName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<EventArgs?> WaitEventAsync(this object value, Func<CancellationToken, Task> func, string eventName, CancellationToken cancellationToken = default)
        {
            try
            {
                var task = value.WaitEventAsync(eventName, cancellationToken);

                await func(cancellationToken).ConfigureAwait(false);

                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

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

            eventInfo.AddEventHandler(instance, @delegate);
        }
    }
}
