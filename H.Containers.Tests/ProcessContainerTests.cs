using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class ProcessContainerTests
    {
        [TestMethod]
        public void StartTest()
        {
            /*
            var receivedException = (Exception?) null;

            using var container = new ProcessContainer("StartTest");
            container.ExceptionOccurred += (sender, exception) => receivedException = exception;

            await container.ClearAsync();

            await container.StartAsync();

            await container.LoadAssemblyAsync("test");

            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1));

                if (receivedException != null)
                {
                    Assert.Fail(receivedException.ToString());
                }
            }*/
        }
    }
}
