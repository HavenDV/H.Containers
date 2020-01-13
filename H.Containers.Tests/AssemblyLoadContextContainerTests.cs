using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class AssemblyLoadContextContainerTests
    {
        [TestMethod]
        public async Task LoadTest()
        {
            using var container = new AssemblyLoadContextContainer("Modules");

            await BaseTests.LoadTestAsync(container, $"{nameof(AssemblyLoadContextContainerTests)}_{nameof(LoadTest)}");
        }
    }
}
