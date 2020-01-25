using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Utilities.Tests
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
            using var factory = new ProxyFactory();
            var instance = factory.CreateInstance<AbstractClass>();

            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");
            instance.Test2();

            Assert.AreEqual(0, result);
        }

        public interface IInterface
        {
            int Test1(string test);
            void Test2();
        }

        [TestMethod]
        public void InterfaceTest()
        {
            using var factory = new ProxyFactory();
            factory.MethodCalled += (sender, args) =>
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
            var instance = factory.CreateInstance<IInterface>();
            
            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");
            instance.Test2();

            Assert.AreEqual(3, result);
        }

        public class CommonClass : IInterface
        {
            public int Test1(string test)
            {
                return 1;
            }

            public void Test2()
            {
                Console.WriteLine("Test2 is completed");
            }
        }

        [TestMethod]
        public void CommonClassTest()
        {
            using var factory = new ProxyFactory();
            factory.MethodCalled += (sender, args) =>
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
            var instance = factory.CreateInstance<CommonClass>();

            //var result = instance.GetType().InvokeMember("Test1", BindingFlags.InvokeMethod, null, instance, new object[] {"hello"});
            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual(1, result);
            instance.Test2();
        }
    }
}
