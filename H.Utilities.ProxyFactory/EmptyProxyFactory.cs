using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace H.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class EmptyProxyFactory : IDisposable
    {
        #region Properties

        private GCHandle GcHandle { get; }

        #endregion

        #region Events

        /// <summary>
        /// 
        /// </summary>
        public virtual event EventHandler<MethodEventArgs>? MethodCalled;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public EmptyProxyFactory()
        {
            GcHandle = GCHandle.Alloc(this);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public Type CreateType(Type baseType)
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

            GenerateMethods(typeBuilder, baseType);
            GenerateEvents(typeBuilder, baseType);

            return typeBuilder.CreateType() ?? throw new InvalidOperationException("Created type is null");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public object CreateInstance(Type baseType)
        {
            var type = CreateType(baseType);

            return Activator.CreateInstance(type, new object[0])
                   ?? throw new InvalidOperationException("Created instance is null");
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

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (GcHandle.IsAllocated)
            {
                GcHandle.Free();
            }
        }

        #endregion

        #region Private methods

        #region Methods

        private List<MethodBuilder> GenerateMethods(TypeBuilder typeBuilder, Type baseType)
        {
            var builders = new List<MethodBuilder>();

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

                builders.Add(methodBuilder);
            }

            return builders;
        }

        private void GenerateMethod(ILGenerator generator, MethodInfo methodInfo)
        {
            generator.Emit(OpCodes.Ldarg_0); // [this]

            var listConstructorInfo = typeof(List<object?>).GetConstructor(Array.Empty<Type>()) ??
                                  throw new InvalidOperationException("Constructor of list is not found");
            generator.Emit(OpCodes.Newobj, listConstructorInfo); // [list]

            var index = 1; // First argument is type
            var addMethodInfo = typeof(List<object?>).GetMethod("Add") ??
                                      throw new InvalidOperationException("Add method is not found");
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
            generator.Emit(OpCodes.Ldc_I8, GCHandle.ToIntPtr(GcHandle).ToInt64()); // [this, list, arg_0, name, address]

            var onMethodCalledInfo = typeof(EmptyProxyFactory).GetMethod(nameof(OnMethodCalled))
                                     ?? throw new InvalidOperationException("Method is null");
            generator.EmitCall(OpCodes.Call, onMethodCalledInfo, 
                new [] { typeof(List<object?>), typeof(object), typeof(string), typeof(long) });

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

            OnMethodCalled(arguments, new object(), "123", GCHandle.ToIntPtr(GCHandle.Alloc(this)).ToInt64());
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <param name="factoryAddress"></param>
        /// <returns></returns>
        public object? OnMethodCalled(List<object?> arguments, object instance, string name, long factoryAddress)
        {
            var factory = GetFactory(factoryAddress);
            var type = instance.GetType();
            var allArgumentsNotNull = arguments.All(argument => argument != null);
            var methodInfo = (allArgumentsNotNull
                                 // ReSharper disable once RedundantEnumerableCastCall
                                 ? type.GetMethod(name, arguments.Cast<object>().Select(argument => argument.GetType()).ToArray())
                                 : null)
                             ?? type.GetMethod(name)
                             ?? throw new InvalidOperationException("Method info is not found");

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

        private List<EventBuilder> GenerateEvents(TypeBuilder typeBuilder, Type baseType)
        {
            var builders = new List<EventBuilder>();

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
                var combine = typeof(Delegate).GetMethod("Combine", new[] { typeof(Delegate), typeof(Delegate) })
                                         ?? throw new InvalidOperationException("Combine method is not found");
                addGenerator.Emit(OpCodes.Ldarg_0);
                addGenerator.Emit(OpCodes.Ldarg_0);
                addGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
                addGenerator.Emit(OpCodes.Ldarg_1);
                addGenerator.Emit(OpCodes.Call, combine);
                addGenerator.Emit(OpCodes.Castclass, handlerType);
                addGenerator.Emit(OpCodes.Stfld, fieldBuilder);
                addGenerator.Emit(OpCodes.Ret);

                eventBuilder.SetAddOnMethod(addMethod); 
                
                var removeMethod = typeBuilder.DefineMethod($"remove_{info.Name}",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    typeof(void),
                    new[] { handlerType });
                var remove = typeof(Delegate).GetMethod("Remove", new[] { typeof(Delegate), typeof(Delegate) })
                             ?? throw new InvalidOperationException("Remove method is not found");
                var removeGenerator = removeMethod.GetILGenerator();
                removeGenerator.Emit(OpCodes.Ldarg_0);
                removeGenerator.Emit(OpCodes.Ldarg_0);
                removeGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
                removeGenerator.Emit(OpCodes.Ldarg_1);
                removeGenerator.Emit(OpCodes.Call, remove);
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

                builders.Add(eventBuilder);
            }

            return builders;
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
            /*
            generator.Emit(OpCodes.Ldarg_0); // [this]
            generator.Emit(OpCodes.Ldfld, fieldInfo); // [event_field]
            generator.Emit(OpCodes.Ldarg_0); // [event_field, this]
            generator.Emit(OpCodes.Ldarg_1); // [event_field, this, EventArgs]

            generator.EmitCall(OpCodes.Callvirt, 
                typeof(EventHandler).GetMethod("Invoke")
                ?? throw new InvalidOperationException("Invoke method is not found"), 
                new [] { typeof(object), typeof(EventArgs) });
                */

            generator.Emit(OpCodes.Ldarg_0); // [this]
            generator.Emit(OpCodes.Ldarg_0); // [this, this]
            generator.Emit(OpCodes.Ldarg_1); // [this, this, args]
            generator.Emit(OpCodes.Ldstr, eventInfo.Name); // [this, this, args, name]
            generator.Emit(OpCodes.Ldc_I8, GCHandle.ToIntPtr(GcHandle).ToInt64()); // [this, this, args, name, address]


            var onEventRaisedInfo = GetMethodInfo(typeof(EmptyProxyFactory), nameof(OnEventRaised));
            generator.EmitCall(OpCodes.Call, onEventRaisedInfo,
                new[] { typeof(object), typeof(object), typeof(string), typeof(long) });

            generator.Emit(OpCodes.Ret);
        }

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
        /// <param name="factoryAddress"></param>
        /// <returns></returns>
        public void OnEventRaised(object instance, object? args, string name, long factoryAddress)
        {
            var factory = GetFactory(factoryAddress);
            var fieldInfo = GetPrivateFieldInfo(instance.GetType(), name);
            var field = fieldInfo?.GetValue(instance);
            if (field != null)
            {
                GetMethodInfo(typeof(EventHandler), "Invoke").Invoke(field, new[] {instance, args});
            }
        }

        private static MethodInfo GetMethodInfo(Type type, string name)
        {
            return type.GetMethod(name)
                   ?? throw new InvalidOperationException($"Method \"{name}\" is not found");
        }

        private static FieldInfo GetPrivateFieldInfo(IReflect type, string name)
        {
            return type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                   ?? throw new InvalidOperationException($"Field \"{name}\" is not found");
        }

        private static EmptyProxyFactory GetFactory(long address)
        {
            var intPtr = new IntPtr(address);
            var gcHandle = GCHandle.FromIntPtr(intPtr);

            if (!gcHandle.IsAllocated)
            {
                throw new InvalidOperationException("Factory is disposed");
            }
            return gcHandle.Target as EmptyProxyFactory
                   ?? throw new InvalidOperationException("Factory is null");
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

                var method = typeof(Task).GetMethod(nameof(Task.FromResult))
                             ?? throw new InvalidOperationException($"{nameof(Task.FromResult)} is not found");
                var genericMethod = method.MakeGenericMethod(taskType);

                var value = Activator.CreateInstance(taskType);
                return genericMethod.Invoke(null, new []{ value });
            }

            return Activator.CreateInstance(type);
        }

        #endregion
    }
}
