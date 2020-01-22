using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace H.Containers
{
    public static class ProxyFactory
    {
        public class Test2
        {
            public void Test()
            {
                Console.WriteLine("hello");
            }
        }

        public static T Create<T>() where T : class
        {
            var type = typeof(T);
            if (!type.IsInterface)
            {
                throw new ArgumentException("Type must be an Interface");
            }

            /*
            var obj = new Test2();
            foreach (var methodInfo in type.GetMethods())
            {
                var method = new DynamicMethod(
                    methodInfo.Name, 
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard, 
                    methodInfo.ReturnType, 
                    methodInfo
                        .GetParameters()
                        .Select(parameter => parameter.ParameterType)
                        .ToArray(), 
                    typeof(ProxyFactory).Module, 
                    false);

                var methodImplementation = typeof(Test2).GetMethod("Test") ?? throw new InvalidOperationException("methodImplementation is null");
                var body = methodImplementation.GetMethodBody() ?? throw new InvalidOperationException("GetMethodBody is null");

                var il = body.GetILAsByteArray();
                var ilInfo = method.GetDynamicILInfo();
                ilInfo.SetCode(il, 32);

                var delegateObject = CreateDelegate(methodInfo, obj);
                method.CreateDelegate(delegateObject.GetType(), obj);
            }*/

            return Unsafe.As<T>(TypeFactory.CreateInstance());
        }

        public static Delegate CreateDelegate(this MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }

            return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
        }
    }
}
