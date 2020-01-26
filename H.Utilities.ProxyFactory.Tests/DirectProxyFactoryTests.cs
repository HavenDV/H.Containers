using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Utilities.Tests
{
    [TestClass]
    public class DirectProxyFactoryTests
    {
        [TestMethod]
        public async Task CommonClassWithInterfaceTest()
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
                    case nameof(EmptyProxyFactoryTests.IInterface.Test2):
                        args.IsCanceled = true;
                        break;

                    case nameof(EmptyProxyFactoryTests.IInterface.Test3Async):
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

            await instance.Test3Async();

            var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(
                async () => await instance.Test4Async(tokenSource.Token));
        }
    }
}
