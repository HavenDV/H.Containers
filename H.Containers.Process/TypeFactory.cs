using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace H.Containers
{
    public static class TypeFactory
    {
        public static Type Create(Type interfaceType)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            var typeBuilder = moduleBuilder.DefineType($"{interfaceType.Name}_ProxyType_{Guid.NewGuid()}", TypeAttributes.Public);

            foreach (var methodInfo in interfaceType.GetMethods())
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
                    typeof(void),
                    parameterTypes);

                var index = 0;
                foreach (var parameterInfo in methodInfo.GetParameters())
                {
                    methodBuilder.DefineParameter(index, parameterInfo.Attributes, parameterInfo.Name);
                    index++;
                }

                typeBuilder.AddInterfaceImplementation(interfaceType);

                var generator = methodBuilder.GetILGenerator();
                GenerateMethod(generator, methodInfo);
            }

            return typeBuilder.CreateType() ?? throw new InvalidOperationException("Created type is null");
        }

        public static void GenerateMethod(ILGenerator generator, MethodInfo methodInfo)
        {
            var listConstructorInfo = typeof(List<object>).GetConstructor(new Type[0]) ??
                                  throw new InvalidOperationException("Constructor of list is not found");
            generator.Emit(OpCodes.Newobj, listConstructorInfo); // [list]

            var index = 1; // First argument is type
            var addMethodInfo = typeof(List<object>).GetMethod("Add") ??
                                      throw new InvalidOperationException("Add method is not found");
            foreach (var _ in methodInfo.GetParameters())
            {
                generator.Emit(OpCodes.Dup); // [list, list]
                generator.Emit(OpCodes.Ldarg, index); // [list, list, arg_i]
                generator.Emit(OpCodes.Callvirt, addMethodInfo); // [list]
                index++;
            }

            generator.DeclareLocal(typeof(List<object>));
            generator.Emit(OpCodes.Stloc_0); // []
            generator.Emit(OpCodes.Ldloc_0); // [list]

            generator.Emit(OpCodes.Ldarg, 0); // [list, arg_0]
            generator.Emit(OpCodes.Ldstr, methodInfo.Name); // [list, arg_0, name]

            var method = typeof(TypeFactory).GetMethod(nameof(RunMethod)) ?? 
                         throw new InvalidOperationException("Method is null");
            generator.EmitCall(OpCodes.Call, method, 
                new [] { typeof(object[]), typeof(Type), typeof(string) });
            
            generator.Emit(OpCodes.Ret);
        }

        public static void RealMethod(object value1, object value2, object value3)
        {
            var arguments = new List<object> {value1, value2, value3};
            
            RunMethod(arguments, new object(), "123");
        }

        public static void RunMethod(List<object> arguments, object instance, string name)
        {
            var type = instance.GetType();

            Console.WriteLine($"Hello, bad man {arguments.FirstOrDefault()} {name}()");
        }

        public static object CreateInstance(Type interfaceType)
        {
            var type = Create(interfaceType);

            return Activator.CreateInstance(type, new object[0])
                   ?? throw new InvalidOperationException("Created instance is null");
        }
    }
}
