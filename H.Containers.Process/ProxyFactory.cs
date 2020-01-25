using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace H.Containers
{
    public static class ProxyFactory
    {
        public static Type CreateType(Type baseType)
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

        public static void GenerateMethod(ILGenerator generator, MethodInfo methodInfo)
        {
            var listConstructorInfo = typeof(List<object?>).GetConstructor(Array.Empty<Type>()) ??
                                  throw new InvalidOperationException("Constructor of list is not found");
            generator.Emit(OpCodes.Newobj, listConstructorInfo); // [list]

            var index = 1; // First argument is type
            var addMethodInfo = typeof(List<object?>).GetMethod("Add") ??
                                      throw new InvalidOperationException("Add method is not found");
            foreach (var _ in methodInfo.GetParameters())
            {
                generator.Emit(OpCodes.Dup); // [list, list]
                generator.Emit(OpCodes.Ldarg, index); // [list, list, arg_i]
                generator.Emit(OpCodes.Callvirt, addMethodInfo); // [list]
                index++;
            }

            generator.Emit(OpCodes.Ldarg_0); // [list, arg_0]
            generator.Emit(OpCodes.Ldstr, methodInfo.Name); // [list, arg_0, name]

            var beforeMethodCalledInfo = typeof(ProxyFactory).GetMethod(nameof(BeforeMethodCalled))
                                     ?? throw new InvalidOperationException("Method is null");
            generator.EmitCall(OpCodes.Call, beforeMethodCalledInfo, 
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

        /*
        public void Generated_Method_Example(object value1, object value2, object value3)
        {
            var arguments = new List<object?> {value1, value2, value3};

            OnMethodCalled(arguments, new object(), "123");
        }
        //*/

        public static event EventHandler<MethodEventArgs>? MethodCalled;

        public static object? BeforeMethodCalled(List<object?> arguments, object instance, string name)
        {
            var type = instance.GetType();
            var methodInfo = type.GetMethod(name, arguments.Select(argument => argument.GetType()).ToArray()) ?? 
                             throw new InvalidOperationException("Method info is not found");

            var args = new MethodEventArgs(arguments, methodInfo)
            {
                ReturnObject = methodInfo.ReturnType != typeof(void)
                               ? Activator.CreateInstance(methodInfo.ReturnType)
                               : null,
            };
            MethodCalled?.Invoke(instance, args);

            return args.ReturnObject;
        }

        public static object CreateInstance(Type baseType)
        {
            var type = CreateType(baseType);

            return Activator.CreateInstance(type, new object[0])
                   ?? throw new InvalidOperationException("Created instance is null");
        }

        public static T CreateInstance<T>() where T : class
        {
            var instance = CreateInstance(typeof(T));
            if (typeof(T).IsInterface)
            {
                return (T) instance;
            }

            return Unsafe.As<T>(instance);
        }
    }
}
