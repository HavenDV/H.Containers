using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Utilities.Tests
{
    [TestClass]
    public class EmptyProxyFactoryTests
    {
        public abstract class AbstractClass
        {
            public abstract int Test1(string test);
            public abstract void Test2();
            public abstract Task Test3Async(CancellationToken cancellationToken = default);
            public abstract Task<int> Test4Async(CancellationToken cancellationToken = default);
        }

        [TestMethod]
        public async Task AbstractTest()
        {
            using var factory = new EmptyProxyFactory();
            var instance = factory.CreateInstance<AbstractClass>();

            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");
            instance.Test2();

            Assert.AreEqual(0, result);

            await instance.Test3Async();
            result = await instance.Test4Async();

            Assert.AreEqual(0, result);
        }

        public interface IInterface
        {
            int Test1(string test);
            void Test2();
            Task Test3Async(CancellationToken cancellationToken = default);
            Task<int> Test4Async(CancellationToken cancellationToken = default);
        }

        [TestMethod]
        public async Task InterfaceTest()
        {
            using var factory = new EmptyProxyFactory();
            factory.MethodCalled += (sender, args) =>
            {
                Console.WriteLine($"MethodCalled: {args.MethodInfo}");

                if (args.Arguments.Any())
                {
                    Console.WriteLine("Arguments:");
                }
                for (var i = 0; i < args.Arguments.Count; i++)
                {
                    Console.WriteLine($"{i}: \"{args.Arguments[i]?.ToString() ?? "null"}\"");
                }

                args.ReturnObject = args.MethodInfo.Name switch
                {
                    "Test1" => 3,
                    "Test4Async" => Task.FromResult(4),
                    _ => args.ReturnObject,
                };
            };
            var instance = factory.CreateInstance<IInterface>();
            
            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");
            instance.Test2();

            Assert.AreEqual(3, result);

            await instance.Test3Async();
            result = await instance.Test4Async();
            Assert.AreEqual(4, result);
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

            public async Task Test3Async(CancellationToken cancellationToken = default)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                Console.WriteLine("Test3Async is completed");
            }

            public async Task<int> Test4Async(CancellationToken cancellationToken = default)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

                return 4;
            }
        }

        [TestMethod]
        public async Task CommonClassTest()
        {
            using var factory = new EmptyProxyFactory();
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

            await instance.Test3Async();
            result = await instance.Test4Async();
            Assert.AreEqual(4, result);
        }
    }
}
