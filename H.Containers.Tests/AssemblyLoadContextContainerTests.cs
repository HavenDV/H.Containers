using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Plugins.Tests.Utilities;

namespace H.Containers.Tests
{
    [TestClass]
    public class AssemblyLoadContextContainerTests
    {
        [TestMethod]
        public void LoadTest()
        {
            const string name = "H.NET.Core.dll";
            var bytes = ResourcesUtilities.ReadFileAsBytes(name);
            var path = Path.Combine(Path.GetTempPath(), $"H.Containers.Tests_{nameof(LoadTest)}_{name}");

            File.WriteAllBytes(path, bytes);

            var container = new AssemblyLoadContext("Modules", true);

            container.LoadFromAssemblyPath(path);

            foreach (var type in container.Assemblies.First().GetTypes())
            {
                Console.WriteLine($"Type: {type.FullName}");
            }

            var containerReference = new WeakReference(container, true);

            container.Unload();

            for (var i = 0; containerReference.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
