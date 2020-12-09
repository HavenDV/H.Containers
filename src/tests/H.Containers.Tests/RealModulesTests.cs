using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class RealModulesTests
    {
        [TestMethod]
        public async Task RssNotifier()
        {
            await BaseModuleTest<INotifier>(
                "H.Notifiers.RssNotifier", 
                "H.Notifiers.RssNotifier",
                "RssNotifier",
                async (instance, cancellationToken) =>
                {
                    instance.SetSetting("IntervalInMilliseconds", "1000");
                    instance.SetSetting("Url", "https://www.upwork.com/ab/feed/topics/rss?securityToken=3046355554bbd7e304e77a4f04ec54ff90dcfe94eb4bb6ce88c120b2a660a42c47a42de8cfd7db2f3f4962ccb8c9a8d1bb2bff326e55b5b464816c9919c4e66c&userUid=749097038387695616&orgUid=749446993539981313");
                    
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                    var value = instance.GetModuleVariableValue("$rss_last_title$");
                    Console.WriteLine($"Rss Last Title: {value}");
                    
                    Assert.IsNotNull(value, nameof(value));
                    Assert.AreNotEqual(string.Empty, value, nameof(value));
                });
        }

        public async Task BaseModuleTest<T>(
            string name, 
            string typeName, 
            string shortName,
            Func<T, CancellationToken, Task> testFunc) 
            where T : class, IModule
        {
            var receivedException = (Exception?)null;
            using var tempDirectory = new TempDirectory(false);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await using var container = new ProcessContainer(nameof(ProcessContainerTests))
            {
                MethodsCancellationToken = cancellationTokenSource.Token,
            };
            container.ExceptionOccurred += (_, exception) =>
            {
                Console.WriteLine($"ExceptionOccurred: {exception}");
                receivedException = exception;

                // ReSharper disable once AccessToDisposedClosure
                cancellationTokenSource.Cancel();
            };

            await container.InitializeAsync(cancellationTokenSource.Token);
            await container.StartAsync(cancellationTokenSource.Token);

            var path = tempDirectory.Extract(name);
            
            await container.LoadAssemblyAsync(path, cancellationTokenSource.Token);
            
            var types = await container.GetTypesAsync(cancellationTokenSource.Token);
            ShowList(types, "Available types");

            using var instance = await container.CreateObjectAsync<T>(typeName, cancellationTokenSource.Token);
            Assert.IsNotNull(instance);
            
            instance.NewCommand += (_, command) =>
            {
                Console.WriteLine($"{nameof(instance.NewCommand)}: {command}");
            };
            instance.ExceptionOccurred += (_, exception) =>
            {
                Console.WriteLine($"{nameof(instance.ExceptionOccurred)}: {exception}");
            };
            instance.LogReceived += (_, log) =>
            {
                Console.WriteLine($"{nameof(instance.LogReceived)}: {log}");
            };
            
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
