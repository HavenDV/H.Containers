using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class AppDomainContainerTests
    {
        [TestMethod]
        public async Task LoadTest()
        {
            using var container = new AppDomainContainer("Modules");

            await Assert.ThrowsExceptionAsync<PlatformNotSupportedException>(async () =>
            {
                await BaseTests.LoadTestAsync(container, $"{nameof(AppDomainContainerTests)}_{nameof(LoadTest)}");
            });
        }
    }
}
