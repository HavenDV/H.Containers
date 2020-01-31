using System;
using System.Threading;
using System.Threading.Tasks;
using H.Utilities.Tests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Utilities.Tests
{
    [TestClass]
    public class RemoteProxyFactoryTests
    {
        [TestMethod]
        public async Task MethodsTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var typeName = typeof(SimpleEventClass).FullName ??
                           throw new InvalidOperationException("Type name is null");
            await BaseTests.BaseInstanceRemoteTestAsync<ISimpleEventClass>(
                typeName,
                (instance, cancellationToken) =>
                {
                    Assert.AreEqual(321 + 123, instance.Method1(123));
                    Assert.AreEqual("Hello, input = 123", instance.Method2("123"));

                    return Task.CompletedTask;
                },
                cancellationTokenSource.Token);
        }

        [TestMethod]
        public async Task EventsTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var typeName = typeof(SimpleEventClass).FullName ??
                           throw new InvalidOperationException("Type name is null");
            await BaseTests.BaseInstanceRemoteTestAsync<ISimpleEventClass>(
                typeName,
                async (instance, cancellationToken) =>
                {
                    instance.Event1 += (sender, value) =>
                    {
                        Console.WriteLine($"Hello, I'm the Event1. My value is {value}");
                    };
                    instance.Event3 += (value) =>
                    {
                        Console.WriteLine($"Hello, I'm the Event3. My value is {value}");
                    };

                    var event1Value = await instance.WaitEventAsync<int>(token =>
                    {
                        instance.RaiseEvent1();

                        return Task.CompletedTask;
                    }, nameof(instance.Event1), cancellationToken);
                    Assert.AreEqual(777, event1Value);

                    var event2Values = await instance.WaitEventAsync(token =>
                    {
                        instance.RaiseEvent3();

                        return Task.CompletedTask;
                    }, nameof(instance.Event3), cancellationToken);
                    Assert.IsNotNull(event2Values);
                    Assert.AreEqual(1, event2Values.Length);
                    Assert.AreEqual("555", event2Values[0]);
                },
                cancellationTokenSource.Token);
        }

        [TestMethod]
        public async Task AsyncMethodsTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var typeName = typeof(CommonClass).FullName ??
                                  throw new InvalidOperationException("Type name is null");
            await BaseTests.BaseInstanceRemoteTestAsync<IInterface>(
                typeName,
                async (instance, cancellationToken) =>
                {
                    await instance.Test3Async(cancellationToken);

                    Assert.AreEqual(4, await instance.Test4Async(cancellationToken));
                },
                cancellationTokenSource.Token);
        }
    }
}
