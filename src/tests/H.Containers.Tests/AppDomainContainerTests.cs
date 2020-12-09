using System;
using System.Threading.Tasks;
using H.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class AppDomainContainerTests
    {
        [TestMethod]
        public async Task LoadTest() => await BaseTests.AsyncTest(TimeSpan.FromMinutes(1), async cancellationToken =>
        {
            using var tempDirectory = new TempDirectory();
            using var container = new AppDomainContainer("Modules");

            await Assert.ThrowsExceptionAsync<PlatformNotSupportedException>(async () =>
            {
                await BaseTests.LoadTestAsync(container, tempDirectory, cancellationToken);
            });
        });
    }
}
