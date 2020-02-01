using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Containers.Tests.Utilities;
using H.NET.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class ProcessContainerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            try
            {
                Application.Clear();
                Application.GetPathAndUnpackIfRequired();
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [TestMethod]
        public async Task StartTest()
        {
            var receivedException = (Exception?) null;
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await using var container = new ProcessContainer(nameof(ProcessContainerTests));
            container.ExceptionOccurred += (sender, exception) =>
            {
                Console.WriteLine($"ExceptionOccurred: {exception}");
                receivedException = exception;

                // ReSharper disable once AccessToDisposedClosure
                cancellationTokenSource.Cancel();
            };

            await container.InitializeAsync(cancellationTokenSource.Token);
            await container.StartAsync(cancellationTokenSource.Token);
            //await container.LoadAssemblyAsync("test", cancellationTokenSource.Token);

            var instance = await container.CreateObjectAsync<ISimpleEventClass>(typeof(SimpleEventClass), cancellationTokenSource.Token);
            instance.Event1 += (sender, args) => Console.WriteLine($"Hello, I'm the event. My value is {args}");

            instance.RaiseEvent1();
            Assert.AreEqual(321 + 123, instance.Method1(123));
            Assert.AreEqual("Hello, input = 123", instance.Method2("123"));

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }

            if (receivedException != null)
            {
                Assert.Fail(receivedException.ToString());
            }
        }

        [TestMethod]
        public async Task RealTest()
        {
            var receivedException = (Exception?)null;
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await using var container = new ProcessContainer(nameof(ProcessContainerTests))
            {
                MethodsCancellationToken = cancellationTokenSource.Token,
            };
            container.ExceptionOccurred += (sender, exception) =>
            {
                Console.WriteLine($"ExceptionOccurred: {exception}");
                receivedException = exception;

                // ReSharper disable once AccessToDisposedClosure
                cancellationTokenSource.Cancel();
            };

            await container.InitializeAsync(cancellationTokenSource.Token);
            await container.StartAsync(cancellationTokenSource.Token);

            var directory = Path.Combine(Path.GetTempPath(), "H.Containers.Tests_YandexConverter");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "YandexConverter.zip");
            var bytes = ResourcesUtilities.ReadFileAsBytes("YandexConverter.zip");
            File.WriteAllBytes(path, bytes);

            ZipFile.ExtractToDirectory(path, directory, true);

            await container.LoadAssemblyAsync(Path.Combine(directory, "YandexConverter.dll"), cancellationTokenSource.Token);

            var instance = await container.CreateObjectAsync<IConverter>("H.NET.Converters.YandexConverter", cancellationTokenSource.Token);
            Assert.IsNotNull(instance);

            Assert.AreEqual("YandexConverter", instance.ShortName);

            var availableSettings = instance.GetAvailableSettings().ToArray();
            Console.WriteLine("AvailableSettings:");
            foreach (var setting in availableSettings)
            {
                Console.WriteLine($" - {setting}");
            }
            CollectionAssert.AreEqual(
                new[] { "FolderId", "OAuthToken", "Lang", "Topic", "ProfanityFilter", "Format", "SampleRateHertz" }, 
                availableSettings);

            //await BaseConvertersTests.ConvertTest(instance, "проверка_проверка_8000.wav", "проверка проверка", cancellationTokenSource.Token);

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }

            if (receivedException != null)
            {
                Assert.Fail(receivedException.ToString());
            }
        }

        public interface ISimpleEventClass
        {
            event EventHandler<int> Event1;

            void RaiseEvent1();
            int Method1(int input);
            string Method2(string input);
        }

        public class SimpleEventClass : ISimpleEventClass
        {
            public event EventHandler<int>? Event1;

            public void RaiseEvent1()
            {
                Event1?.Invoke(this, 777);
            }

            public int Method1(int input)
            {
                return 321 + input;
            }

            public string Method2(string input)
            {
                return $"Hello, input = {input}";
            }
        }
    }
}
