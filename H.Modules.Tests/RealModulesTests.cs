using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Containers;
using H.Modules.Tests.Utilities;
using H.NET.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Modules.Tests
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
        [Ignore]
        public async Task RssNotifier()
        {
            await BaseModuleTest(
                "H.Notifiers.RssNotifier", 
                "H.Notifiers.RssNotifier",
                "RssNotifier",
                async (instance, token) =>
                {
                    instance.SetSetting("IntervalInMilliseconds", "1000");
                    instance.SetSetting("Url", "https://www.upwork.com/ab/feed/topics/rss?securityToken=3046355554bbd7e304e77a4f04ec54ff90dcfe94eb4bb6ce88c120b2a660a42c47a42de8cfd7db2f3f4962ccb8c9a8d1bb2bff326e55b5b464816c9919c4e66c&userUid=749097038387695616&orgUid=749446993539981313");
                    
                    await Task.Delay(TimeSpan.FromSeconds(5), token);

                    var value = instance.GetModuleVariableValue("$rss_last_title$");
                    Console.WriteLine($"Rss Last Title: {value}");
                });
        }

        public async Task BaseModuleTest(
            string name, 
            string typeName, 
            string shortName,
            Func<IModule, CancellationToken, Task> testFunc) 
        {
            var receivedException = (Exception?)null;
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            using var manager = new ModuleManager<IModule>(
                Path.Combine(Path.GetTempPath(), $"H.Containers.Tests_{name}"));
            manager.ExceptionOccurred += (sender, exception) =>
            {
                Console.WriteLine($"ExceptionOccurred: {exception}");
                receivedException = exception;

                // ReSharper disable once AccessToDisposedClosure
                cancellationTokenSource.Cancel();
            };

            var bytes = ResourcesUtilities.ReadFileAsBytes($"{name}.zip");

            using var instance = await manager.AddModuleAsync<ProcessContainer>(
                name, 
                typeName, 
                bytes, 
                container => container.LaunchInCurrentProcess = true,
                cancellationTokenSource.Token);

            Assert.IsNotNull(instance);

            var types = await manager.GetTypesAsync(cancellationTokenSource.Token);
            ShowList(types, "Available types");

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
