using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace H.Containers
{
    public static class TypeFactory
    {
        public class Test2
        {
            public void Test()
            {
                Console.WriteLine("hello");
            }
        }

        public static Type Create()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MyModule");
            var typeBuilder = moduleBuilder.DefineType("MyType");
            var methodBuilder = typeBuilder.DefineMethod("Test",
                                    MethodAttributes.Public | MethodAttributes.Static,
                                    typeof(void),
                                    new Type[] {});

            var methodImplementation = typeof(Test2).GetMethod("Test") ?? throw new InvalidOperationException("methodImplementation is null");
            var body = methodImplementation.GetMethodBody() ?? throw new InvalidOperationException("GetMethodBody is null");

            var il = body.GetILAsByteArray();

            Expression<Action> expression = () => Console.WriteLine("hello");
            expression.CompileToMethod(methodBuilder);
             
            var codes = new byte[] {
                0x02,   /* 02h is the opcode for ldarg.0 */
                0x03,   /* 03h is the opcode for ldarg.1 */
                0x58,   /* 58h is the opcode for add     */
                0x2A    /* 2Ah is the opcode for ret     */
            };

            //methodBuilder.CreateMethodBody(il, il.Length);

            return typeBuilder.CreateType();
        }

        public static object CreateInstance()
        {
            var type = Create();

            var instance = Activator.CreateInstance(type, new object[0]); 
            var value = type.InvokeMember("Test",
                BindingFlags.InvokeMethod,
                null,
                instance,
                new object[] {});

            return instance;
        }
    }
}
