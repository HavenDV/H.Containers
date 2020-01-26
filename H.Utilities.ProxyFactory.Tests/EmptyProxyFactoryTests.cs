﻿using System;
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
            public abstract int Property1 { get; set; }
            public abstract int Property2 { get; }

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

            Assert.AreEqual(0, instance.Test1("hello"));
            instance.Test2();
            await instance.Test3Async();
            Assert.AreEqual(0, await instance.Test4Async());

            Assert.AreEqual(0, instance.Property1);
            instance.Property1 = 5;
            Assert.AreEqual(0, instance.Property1);
            Assert.AreEqual(0, instance.Property2);
        }

        public interface IInterface
        {
            int Property1 { get; set; }
            int Property2 { get; }

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
                    nameof(IInterface.Test1) => 3,
                    nameof(IInterface.Test4Async) => Task.FromResult(4),
                    "get_" + nameof(IInterface.Property1) => 11,
                    _ => args.ReturnObject,
                };
            };
            var instance = factory.CreateInstance<IInterface>();

            Assert.AreEqual(3, instance.Test1("hello"));
            instance.Test2();
            await instance.Test3Async();
            Assert.AreEqual(4, await instance.Test4Async());
            
            Assert.AreEqual(11, instance.Property1);
            instance.Property1 = 5;
            Assert.AreEqual(11, instance.Property1);
            Assert.AreEqual(0, instance.Property2);
        }

        public class CommonClass : IInterface
        {
            public int Property1 { get; set; } = 1;
            public int Property2 { get; } = 2;

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

                args.ReturnObject = args.MethodInfo.Name switch
                {
                    nameof(IInterface.Test1) => 3,
                    nameof(IInterface.Test4Async) => Task.FromResult(44),
                    _ => args.ReturnObject,
                };
            };
            var instance = factory.CreateInstance<CommonClass>();

            Assert.AreEqual(1, instance.Test1("hello"));
            instance.Test2();
            await instance.Test3Async();
            Assert.AreEqual(4, await instance.Test4Async());

            Assert.AreEqual(0, instance.Property1);
            instance.Property1 = 5;
            Assert.AreEqual(5, instance.Property1);
            Assert.AreEqual(0, instance.Property2);
        }
    }
}
