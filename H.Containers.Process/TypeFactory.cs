using System;
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
            var typeBuilder = moduleBuilder.DefineType($"{interfaceType.Name}_ProxyType", TypeAttributes.Public);

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
                typeBuilder.AddInterfaceImplementation(interfaceType);

                var method = typeof(TypeFactory).GetMethod(nameof(RunMethod)) ?? throw new InvalidOperationException("Method is null");
                var generator = methodBuilder.GetILGenerator();
                //generator.Emit(OpCodes.Ldstr, "Hello, World!");
                generator.EmitCall(OpCodes.Call, method, new Type[] { });
                generator.Emit(OpCodes.Ret);
            }

            return typeBuilder.CreateType() ?? throw new InvalidOperationException("Created type is null");
        }

        public static void RunMethod()
        {
            Console.WriteLine("Hello, bad man");
        }

        public static object CreateInstance(Type interfaceType)
        {
            var type = Create(interfaceType);

            return Activator.CreateInstance(type, new object[0])
                   ?? throw new InvalidOperationException("Created instance is null");
        }
    }
}
