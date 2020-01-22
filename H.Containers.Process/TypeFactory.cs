using System;
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
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MyModule");
            var typeBuilder = moduleBuilder.DefineType("MyType", TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineMethod("Test",
                                    MethodAttributes.Public | 
                                    MethodAttributes.HideBySig |
                                    MethodAttributes.Final |
                                    MethodAttributes.Virtual |
                                    MethodAttributes.NewSlot, // | MethodAttributes.Static
                                    typeof(void),
                                    new Type[] {});
            typeBuilder.AddInterfaceImplementation(interfaceType);

            var method = typeof(TypeFactory).GetMethod("Hello") ?? throw new InvalidOperationException("Method is null");
            var generator = methodBuilder.GetILGenerator();
            //generator.Emit(OpCodes.Ldstr, "Hello, World!");
            generator.EmitCall(OpCodes.Call, method, new Type []{ });
            generator.Emit(OpCodes.Ret);

            return typeBuilder.CreateType() ?? throw new InvalidOperationException("Created type is null");
        }

        public static void Hello()
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
