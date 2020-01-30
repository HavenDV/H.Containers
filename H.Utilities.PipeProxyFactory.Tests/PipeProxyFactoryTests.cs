using System;
using System.Threading;
using System.Threading.Tasks;
using H.Utilities.Tests.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Utilities.Tests
{
    [TestClass]
    public class PipeProxyFactoryTests
    {
        [TestMethod]
        public async Task MethodsTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var typeName = typeof(SimpleEventClass).FullName ??
                           throw new InvalidOperationException("Type name is null");
            await BaseTests.BaseInstanceTestAsync<ISimpleEventClass>(
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
            await BaseTests.BaseInstanceTestAsync<ISimpleEventClass>(
                typeName,
                async (instance, cancellationToken) =>
                {
                    instance.Event1 += (sender, value) => Console.WriteLine($"Hello, I'm the Event1. My value is {value}");
                    instance.Event3 += (value) => Console.WriteLine($"Hello, I'm the Event3. My value is {value}");

                    await instance.WaitEventAsync<int>(token =>
                    {
                        instance.RaiseEvent1();

                        return Task.CompletedTask;
                    }, nameof(instance.Event1), cancellationToken);

                    await instance.WaitEventAsync(token =>
                    {
                        instance.RaiseEvent3();

                        return Task.CompletedTask;
                    }, nameof(instance.Event3), cancellationToken);
                },
                cancellationTokenSource.Token);
        }
    }
}
