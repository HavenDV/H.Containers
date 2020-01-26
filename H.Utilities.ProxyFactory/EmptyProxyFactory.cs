using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace H.Utilities
{
    public class EmptyProxyFactory : IDisposable
    {
        private GCHandle GcHandle { get; }

        public virtual event EventHandler<MethodEventArgs>? MethodCalled;

        public EmptyProxyFactory()
        {
            GcHandle = GCHandle.Alloc(this);
        }

        public Type CreateType(Type baseType)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            var typeBuilder = moduleBuilder.DefineType($"{baseType.Name}_ProxyType_{Guid.NewGuid()}", TypeAttributes.Public);

            foreach (var methodInfo in baseType.GetMethods())
            {
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

                if (baseType.IsInterface)
                {
                    typeBuilder.AddInterfaceImplementation(baseType);
                }

                var generator = methodBuilder.GetILGenerator();
                GenerateMethod(generator, methodInfo);
            }

            return typeBuilder.CreateType() ?? throw new InvalidOperationException("Created type is null");
        }

        public void GenerateMethod(ILGenerator generator, MethodInfo methodInfo)
        {
            generator.Emit(OpCodes.Ldarg_0); // [this]

            var listConstructorInfo = typeof(List<object?>).GetConstructor(Array.Empty<Type>()) ??
                                  throw new InvalidOperationException("Constructor of list is not found");
            generator.Emit(OpCodes.Newobj, listConstructorInfo); // [list]

            var index = 1; // First argument is type
            var addMethodInfo = typeof(List<object?>).GetMethod("Add") ??
                                      throw new InvalidOperationException("Add method is not found");
            foreach (var _ in methodInfo.GetParameters())
            {
                generator.Emit(OpCodes.Dup); // [this, list, list]
                generator.Emit(OpCodes.Ldarg, index); // [this, list, list, arg_i]
                generator.Emit(OpCodes.Callvirt, addMethodInfo); // [this, list]
                index++;
            }

            generator.Emit(OpCodes.Ldarg_0); // [this, list, arg_0]
            generator.Emit(OpCodes.Ldstr, methodInfo.Name); // [this, list, arg_0, name]
            generator.Emit(OpCodes.Ldc_I8, GCHandle.ToIntPtr(GcHandle).ToInt64()); // [this, list, arg_0, name, address]

            var onMethodCalledInfo = typeof(EmptyProxyFactory).GetMethod(nameof(OnMethodCalled))
                                     ?? throw new InvalidOperationException("Method is null");
            generator.EmitCall(OpCodes.Call, onMethodCalledInfo, 
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

        public void Generated_Method_Example(object value1, object value2, object value3)
        {
            var arguments = new List<object?> {value1, value2, value3};

            OnMethodCalled(arguments, new object(), "123", GCHandle.ToIntPtr(GCHandle.Alloc(this)).ToInt64());
        }

        public object? OnMethodCalled(List<object?> arguments, object instance, string name, long factoryAddress)
        {
            var intPtr = new IntPtr(factoryAddress);
            var gcHandle = GCHandle.FromIntPtr(intPtr);

            if (!gcHandle.IsAllocated)
            {
                throw new InvalidOperationException("Factory is disposed");
            }
            var factory = gcHandle.Target as EmptyProxyFactory
                          ?? throw new InvalidOperationException("Factory is null");
            var type = instance.GetType();
            var allArgumentsNotNull = arguments.All(argument => argument != null);
            var methodInfo = (allArgumentsNotNull
                                 // ReSharper disable once RedundantEnumerableCastCall
                                 ? type.GetMethod(name, arguments.Cast<object>().Select(argument => argument.GetType()).ToArray())
                                 : type.GetMethod(name))
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

        public object CreateInstance(Type baseType)
        {
            var type = CreateType(baseType);

            return Activator.CreateInstance(type, new object[0])
                   ?? throw new InvalidOperationException("Created instance is null");
        }

        public T CreateInstance<T>() where T : class
        {
            var instance = CreateInstance(typeof(T));
            if (typeof(T).IsInterface)
            {
                return (T) instance;
            }

            return Unsafe.As<T>(instance);
        }

        public void Dispose()
        {
            if (GcHandle.IsAllocated)
            {
                GcHandle.Free();
            }
        }
    }
}
