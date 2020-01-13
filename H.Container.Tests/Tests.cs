using System;
using System.Threading.Tasks;
using H.Containers.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Container.Tests
{
    [TestClass]
    public class DataTests
    {
        [TestMethod]
        public async Task StartTest()
        {
            var receivedException = (Exception?) null;

            using var container = new ProcessContainer();
            container.ExceptionOccurred += (sender, exception) => receivedException = exception;

            await container.ClearAsync();

            await container.StartAsync();

            await container.LoadAssemblyAsync("test");

            for (var i = 0; i < 1000; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1));

                if (receivedException != null)
                {
                    Assert.Fail(receivedException.ToString());
                }
            }
        }
    }
}
