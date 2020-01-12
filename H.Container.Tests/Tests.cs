using System.Threading.Tasks;
using H.Pipes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Container.Tests
{
    [TestClass]
    public class DataTests
    {
        #region Tests

        [TestMethod]
        public async Task StartDisposeStart()
        {
            {
                await using var pipe = new PipeServer<string>("test");
                await pipe.StartAsync();
            }
            {
                await using var pipe = new PipeServer<string>("test");
                await pipe.StartAsync();
            }
        }

#endregion
    }
}
