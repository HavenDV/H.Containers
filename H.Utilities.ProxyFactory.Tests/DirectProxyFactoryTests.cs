using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Utilities.Tests
{
    [TestClass]
    public class DirectProxyFactoryTests
    {
        [TestMethod]
        public void CommonClassWithInterfaceTest()
        {
            using var factory = new DirectProxyFactory();
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

                switch (args.MethodInfo.Name)
                {
                    case "Test2":
                        args.IsCanceled = true;
                        break;
                }
            };
            factory.MethodCompleted += (sender, args) =>
            {
                Console.WriteLine($"MethodCompleted: {args.MethodInfo}");

                if (args.Arguments.Any())
                {
                    Console.WriteLine("Arguments:");
                }
                for (var i = 0; i < args.Arguments.Count; i++)
                {
                    Console.WriteLine($"{i}: \"{args.Arguments[i]}\"");
                }
            };
            var instance = factory.CreateInstance<EmptyProxyFactoryTests.IInterface>(new EmptyProxyFactoryTests.CommonClass());

            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual(1, result);
            instance.Test2();
        }
    }
}
