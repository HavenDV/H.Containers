using System;
using System.Threading.Tasks;
using H.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class AssemblyLoadContextContainerTests
    {
        [TestMethod]
        public async Task LoadTest() => await BaseTests.AsyncTest(TimeSpan.FromMinutes(1), async cancellationToken =>
        {
            using var tempDirectory = new TempDirectory();
            await using var container = new AssemblyLoadContextContainer("Modules");

            await BaseTests.LoadTestAsync(container, tempDirectory, cancellationToken);
        });
    }
}
