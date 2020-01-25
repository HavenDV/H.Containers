using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class ProxyFactoryTests
    {
        public abstract class AbstractClass
        {
            public abstract int Test1(string test);
            public abstract void Test2();
        }

        [TestMethod]
        public void AbstractTest()
        {
            var instance = ProxyFactory.CreateInstance<AbstractClass>();

            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");
            instance.Test2();
        }

        public interface IInterface
        {
            int Test1(string test);
            void Test2();
        }

        [TestMethod]
        public void InterfaceTest()
        {
            ProxyFactory.MethodCalled += (sender, args) =>
            {
                Console.WriteLine($"MethodCalled: {args.MethodInfo}");

                if (args.Arguments.Any())
                {
                    Console.WriteLine("Arguments:");
                }
                for (var i = 0; i < args.Arguments.Count; i++)
                {
                    Console.WriteLine($"{i}: \"{args.Arguments[i]}\"");
                }

                args.ReturnObject = args.MethodInfo.ReturnType == typeof(int) ? 3 : args.ReturnObject;
            };
            var instance = ProxyFactory.CreateInstance<IInterface>();

            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");
            instance.Test2();
        }
    }
}
