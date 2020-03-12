using System;
using System.Collections.Generic;
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
    public class RealModulesTests
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
        public async Task RssNotifier()
        {
            await BaseModuleTest<INotifier>(
                "H.Notifiers.RssNotifier", 
                "H.NET.Notifiers.RssNotifier",
                "RssNotifier",
                (instance, token) =>
                {
                    return Task.CompletedTask;
                });
        }

        [TestMethod]
        public async Task BaseModuleTest<T>(
            string name, 
            string typeName, 
            string shortName,
            Func<T, CancellationToken, Task> testFunc) 
            where T : class, IModule
        {
            var receivedException = (Exception?)null;
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
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

            var directory = Path.Combine(Path.GetTempPath(), $"H.Containers.Tests_{name}");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"{name}.zip");
            var bytes = ResourcesUtilities.ReadFileAsBytes($"{name}.zip");
            File.WriteAllBytes(path, bytes);

            ZipFile.ExtractToDirectory(path, directory, true);

            await container.LoadAssemblyAsync(Path.Combine(directory, $"{name}.dll"), cancellationTokenSource.Token);
            
            var types = await container.GetTypesAsync(cancellationTokenSource.Token);
            ShowList(types, "Available types");

            var instance = await container.CreateObjectAsync<T>(typeName, cancellationTokenSource.Token);
            Assert.IsNotNull(instance);

            Assert.AreEqual(shortName, instance.ShortName);

            var availableSettings = instance.GetAvailableSettings().ToArray();
            ShowList(availableSettings, "Available settings");

            await testFunc(instance, cancellationTokenSource.Token);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }

            if (receivedException != null)
            {
                Assert.Fail(receivedException.ToString());
            }
        }

        private static void ShowList<T>(ICollection<T> list, string name)
        {
            Console.WriteLine($"{name}: {list.Count}");
            foreach (var value in list)
            {
                Console.WriteLine($" - {value}");
            }

            Console.WriteLine();
        }
    }
}
