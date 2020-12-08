using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class CurrentDomainContainerTests
    {
        [TestMethod]
        public async Task LoadTest() => await BaseTests.AsyncTest(TimeSpan.FromMinutes(1), async cancellationToken =>
        {
            using var tempDirectory = new TempDirectory(false);
            using var container = new CurrentDomainContainer("Modules");

            await BaseTests.LoadTestAsync(container, tempDirectory, cancellationToken);
        });
    }
}
