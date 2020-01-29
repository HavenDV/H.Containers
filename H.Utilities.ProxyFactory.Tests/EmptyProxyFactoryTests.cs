using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Utilities.Extensions;
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
            public abstract void RaiseEvent1();
        }

        [TestMethod]
        public async Task AbstractTest()
        {
            var factory = CreateFactory();
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

        public interface IBaseInterface
        {
            int Property1 { get; set; }

            event EventHandler Event1;

            int Test1(string test);
            Task<int> Test4Async(CancellationToken cancellationToken = default);
            void RaiseEvent1();
        }

        public interface IInterface : IBaseInterface
        {
            int Property2 { get; }

            event EventHandler<int> Event2;

            void Test2();
            Task Test3Async(CancellationToken cancellationToken = default);
            void RaiseEvent2();
        }

        [TestMethod]
        public async Task InterfaceTest()
        {
            var factory = CreateFactory();
            factory.MethodCalled += (sender, args) =>
            {
                args.ReturnObject = args.MethodInfo.Name switch
                {
                    nameof(IInterface.Test1) => 3,
                    nameof(IInterface.Test4Async) => Task.FromResult(4),
                    "get_" + nameof(IInterface.Property1) => 11,
                    _ => args.ReturnObject,
                };
            };
            factory.EventRaised += (sender, args) =>
            {
                args.IsCanceled = args.EventInfo.Name switch
                {
                    nameof(IInterface.Event1) => true,
                    _ => false,
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
            Assert. AreEqual(0, instance.Property2);

            instance.Event1 += (sender, args) => Console.WriteLine("Event1");
            instance.RaiseEvent(nameof(IInterface.Event1), EventArgs.Empty);
            instance.RaiseEvent1(); // it's empty
            instance.RaiseEvent2(); // it's empty
        }

        public class CommonClass : IInterface
        {
            public int Property1 { get; set; } = 1;
            public int Property2 { get; } = 2;

            public event EventHandler? Event1;
            public event EventHandler<int>? Event2;

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

            public void RaiseEvent1()
            {
                Event1?.Invoke(this, EventArgs.Empty);
            }

            public void RaiseEvent2()
            {
                Event2?.Invoke(this, 777);
            }
        }

        [TestMethod]
        public async Task CommonClassTest()
        {
            var factory = CreateFactory();
            factory.MethodCalled += (sender, args) =>
            {
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

        private static EmptyProxyFactory CreateFactory()
        {
            var factory = new EmptyProxyFactory();
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
            };
            factory.EventRaised += (sender, args) =>
            {
                Console.WriteLine($"EventRaised: {args.EventInfo}");
            };
            factory.EventCompleted += (sender, args) =>
            {
                Console.WriteLine($"EventCompleted: {args.EventInfo}");
            };

            return factory;
        }
    }
}
