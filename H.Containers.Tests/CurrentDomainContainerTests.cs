using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class CurrentDomainContainerTests
    {
        [TestMethod]
        public async Task LoadTest()
        {
            using var container = new CurrentDomainContainer("Modules");

            await BaseTests.LoadTestAsync(container, $"{nameof(CurrentDomainContainerTests)}_{nameof(LoadTest)}");
        }
    }
}
