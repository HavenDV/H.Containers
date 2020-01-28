using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using H.Utilities.Args;
using H.Utilities.Extensions;

namespace H.Utilities
{
    /// <summary>
    /// Creates empty copy of selected class
    /// All methods will be have empty body and returns default body
    /// All events will be implemented, but can be raised only by user
    /// All properties does not have inner fields, but get/set methods is implemented as empty body methods
    /// </summary>
    public class EmptyProxyFactory
    {
        #region Constants

        public const string ProxyFactoryFieldName = "_proxyFactory";

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        public virtual event EventHandler<MethodEventArgs>? MethodCalled;

        /// <summary>
        /// 
        /// </summary>
        public virtual event EventHandler<EventEventArgs>? EventRaised;

        /// <summary>
        /// 
        /// </summary>
        public virtual event EventHandler<EventEventArgs>? EventCompleted;

        #endregion

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public object CreateInstance(Type baseType)
        {
            var type = CreateType(baseType);

            var instance = Activator.CreateInstance(type, new object[0])
                                  ?? throw new InvalidOperationException("Created instance is null");

            type.GetPrivateFieldInfo(ProxyFactoryFieldName)
                .SetValue(instance, this);

            return instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T CreateInstance<T>() where T : class
        {
            var instance = CreateInstance(typeof(T));
            if (typeof(T).IsInterface)
            {
                return (T)instance;
            }

            return Unsafe.As<T>(instance);
        }

        #endregion

        #region Private methods

        private Type CreateType(Type baseType)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            var typeBuilder = moduleBuilder.DefineType($"{baseType.Name}_ProxyType_{Guid.NewGuid()}", TypeAttributes.Public);
            if (baseType.IsInterface)
            {
                typeBuilder.AddInterfaceImplementation(baseType);
            }

            typeBuilder.DefineField(ProxyFactoryFieldName, typeof(EmptyProxyFactory), FieldAttributes.Private);

            GenerateMethods(typeBuilder, baseType);
            GenerateEvents(typeBuilder, baseType);

            return typeBuilder.CreateType() ?? throw new InvalidOperationException("Created type is null");
        }

        #region Methods

        private void GenerateMethods(TypeBuilder typeBuilder, Type baseType)
        {
            var ignoredMethods = new List<string>();
            ignoredMethods.AddRange(baseType.GetEvents().Select(i => $"add_{i.Name}"));
            ignoredMethods.AddRange(baseType.GetEvents().Select(i => $"remove_{i.Name}"));

            foreach (var methodInfo in baseType.GetMethods())
            {
                if (ignoredMethods.Contains(methodInfo.Name))
                {
                    continue;
                }

                var parameterTypes = methodInfo
                    .GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
                var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.Final |
                    MethodAttributes.Virtual |
                    MethodAttributes.NewSlot,
                    methodInfo.ReturnType,
                    parameterTypes);

                var index = 0;
                foreach (var parameterInfo in methodInfo.GetParameters())
                {
                    methodBuilder.DefineParameter(index, parameterInfo.Attributes, parameterInfo.Name);
                    index++;
                }

                var generator = methodBuilder.GetILGenerator();
                GenerateMethod(generator, methodInfo);
            }
        }

        private void GenerateMethod(ILGenerator generator, MethodInfo methodInfo)
        {
            generator.Emit(OpCodes.Ldarg_0); // [this]

            var listConstructorInfo = typeof(List<object?>).GetConstructor(Array.Empty<Type>()) ??
                                  throw new InvalidOperationException("Constructor of list is not found");
            generator.Emit(OpCodes.Newobj, listConstructorInfo); // [this, list]

            var index = 1; // First argument is type
            var addMethodInfo = typeof(List<object?>).GetMethodInfo(nameof(List<object?>.Add));
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                generator.Emit(OpCodes.Dup); // [this, list, list]

                generator.Emit(OpCodes.Ldarg, index); // [this, list, list, arg_i]
                if (parameterInfo.ParameterType.IsValueType)
                {
                    generator.Emit(OpCodes.Box, parameterInfo.ParameterType); // [this, list, list, arg_i]
                }

                generator.Emit(OpCodes.Callvirt, addMethodInfo); // [this, list]
                index++;
            }

            generator.Emit(OpCodes.Ldarg_0); // [this, list, arg_0]
            generator.Emit(OpCodes.Ldstr, methodInfo.Name); // [this, list, arg_0, name]
            
            generator.EmitCall(OpCodes.Call,
                typeof(EmptyProxyFactory).GetMethodInfo(nameof(OnMethodCalled)), 
                new [] { typeof(List<object?>), typeof(object), typeof(string) });

            if (methodInfo.ReturnType != typeof(void))
            {
                generator.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
            }
            else
            {
                generator.Emit(OpCodes.Pop);
            }

            generator.Emit(OpCodes.Ret);
        }

        // ReSharper disable once UnusedMember.Local
        private void Generated_Method_Example(object value1, object value2, CancellationToken cancellationToken = default)
        {
            var arguments = new List<object?> {value1, value2, cancellationToken};

            OnMethodCalled(arguments, new object(), "123");
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public object? OnMethodCalled(List<object?> arguments, object instance, string name)
        {
            var type = instance.GetType();
            var factory = type.GetPrivateFieldInfo(ProxyFactoryFieldName).GetValue(instance) as EmptyProxyFactory
                          ?? throw new InvalidOperationException($"{ProxyFactoryFieldName} is null");
            var allArgumentsNotNull = arguments.All(argument => argument != null);
            var methodInfo = type.GetMethodInfo(name, 
                allArgumentsNotNull
                    // ReSharper disable once RedundantEnumerableCastCall
                    ? arguments.Cast<object>().Select(argument => argument.GetType()).ToArray()
                    : null);

            var args = new MethodEventArgs(arguments, methodInfo, factory)
            {
                ReturnObject = CreateReturnObject(methodInfo),
            };
            factory.MethodCalled?.Invoke(instance, args);

            if (args.Exception != null)
            {
                throw args.Exception;
            }

            return args.ReturnObject;
        }

        #endregion

        #region Events

        private void GenerateEvents(TypeBuilder typeBuilder, Type baseType)
        {
            foreach (var info in baseType.GetEvents())
            {
                var handlerType = // ReSharper disable once ConstantNullCoalescingCondition
                    info.EventHandlerType ?? throw new InvalidOperationException("EventHandlerType is null");
                var eventType = GetEventType(handlerType);

                var fieldBuilder = typeBuilder.DefineField(info.Name, handlerType, FieldAttributes.Private);
                var eventBuilder = typeBuilder.DefineEvent(info.Name, info.Attributes, handlerType);

                var addMethod = typeBuilder.DefineMethod($"add_{info.Name}",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    typeof(void),
                    new[] { handlerType });
                var addGenerator = addMethod.GetILGenerator();
                addGenerator.Emit(OpCodes.Ldarg_0);
                addGenerator.Emit(OpCodes.Ldarg_0);
                addGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
                addGenerator.Emit(OpCodes.Ldarg_1);
                addGenerator.Emit(OpCodes.Call,
                    typeof(Delegate).GetMethodInfo(nameof(Delegate.Combine), new[] { typeof(Delegate), typeof(Delegate) }));
                addGenerator.Emit(OpCodes.Castclass, handlerType);
                addGenerator.Emit(OpCodes.Stfld, fieldBuilder);
                addGenerator.Emit(OpCodes.Ret);

                eventBuilder.SetAddOnMethod(addMethod); 
                
                var removeMethod = typeBuilder.DefineMethod($"remove_{info.Name}",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    typeof(void),
                    new[] { handlerType });
                var removeGenerator = removeMethod.GetILGenerator();
                removeGenerator.Emit(OpCodes.Ldarg_0);
                removeGenerator.Emit(OpCodes.Ldarg_0);
                removeGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
                removeGenerator.Emit(OpCodes.Ldarg_1);
                removeGenerator.Emit(OpCodes.Call, typeof(Delegate).GetMethodInfo(nameof(Delegate.Remove)));
                removeGenerator.Emit(OpCodes.Castclass, handlerType);
                removeGenerator.Emit(OpCodes.Stfld, fieldBuilder);
                removeGenerator.Emit(OpCodes.Ret);
                eventBuilder.SetRemoveOnMethod(removeMethod);

                var onMethodBuilder = typeBuilder.DefineMethod($"On{info.Name}",
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.Final |
                    MethodAttributes.Virtual |
                    MethodAttributes.NewSlot,
                    typeof(void),
                    new []{ eventType });
                
                var generator = onMethodBuilder.GetILGenerator();
                GenerateOnEventMethod(generator, info);

                eventBuilder.SetRaiseMethod(onMethodBuilder);
            }
        }

        private Type GetEventType(Type handlerType)
        {
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

        private void GenerateOnEventMethod(ILGenerator generator, EventInfo eventInfo)
        {
            generator.Emit(OpCodes.Ldarg_0); // [this]
            generator.Emit(OpCodes.Ldarg_0); // [this, this]
            generator.Emit(OpCodes.Ldarg_1); // [this, this, args]
            generator.Emit(OpCodes.Ldstr, eventInfo.Name); // [this, this, args, name]

            generator.EmitCall(OpCodes.Call,
                typeof(EmptyProxyFactory).GetMethodInfo(nameof(OnEventRaised)),
                new[] { typeof(object), typeof(object), typeof(string) });

            generator.Emit(OpCodes.Ret);
        }

        // ReSharper disable once EventNeverSubscribedTo.Local
        private event EventHandler? OnEvent;

        // ReSharper disable once UnusedMember.Local
        private void Generated_OnEvent_Example(EventArgs args)
        {
            OnEvent?.Invoke(this, args);
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="args"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public void OnEventRaised(object instance, object? args, string name)
        {
            var type = instance.GetType();
            var factory = type.GetPrivateFieldInfo(ProxyFactoryFieldName).GetValue(instance) as EmptyProxyFactory
                          ?? throw new InvalidOperationException($"{ProxyFactoryFieldName} is null");

            var eventEventArgs = new EventEventArgs(args, type.GetEventInfo(name), factory);
            factory.EventRaised?.Invoke(instance, eventEventArgs);

            if (eventEventArgs.IsCanceled)
            {
                return;
            }

            var field = type
                .GetPrivateFieldInfo(name)
                .GetValue(instance);
            if (field != null)
            {
                typeof(EventHandler).GetMethodInfo(nameof(EventHandler.Invoke))
                    .Invoke(field, new[] {instance, args});
            }

            factory.EventCompleted?.Invoke(instance, eventEventArgs);
        }

        #endregion

        private object? CreateReturnObject(MethodInfo methodInfo)
        {
            var type = methodInfo.ReturnType;

            if (type == typeof(void))
            {
                return null;
            }
            if (type == typeof(Task))
            {
                return Task.CompletedTask;
            }
            if (type.BaseType == typeof(Task))
            {
                var taskType = type.GenericTypeArguments.FirstOrDefault()
                               ?? throw new InvalidOperationException("Task type is null");
                var value = Activator.CreateInstance(taskType);

                return typeof(Task).GetMethodInfo(nameof(Task.FromResult))
                    .MakeGenericMethod(taskType)
                    .Invoke(null, new []{ value });
            }

            return Activator.CreateInstance(type);
        }

        #endregion
    }
}
