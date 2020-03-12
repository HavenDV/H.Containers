using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using H.Containers.Tests.Utilities;

namespace H.Containers.Tests
{
    public static class BaseTests
    {
        public static async Task LoadTestAsync(IContainer container, string testName, CancellationToken cancellationToken = default)
        {
            const string name = "H.NET.Core.dll";
            var bytes = ResourcesUtilities.ReadFileAsBytes(name);
            var path = Path.Combine(Path.GetTempPath(), $"H.Containers.Tests_{testName}_{name}");

            File.WriteAllBytes(path, bytes);

            await container.StartAsync(cancellationToken);

            await container.LoadAssemblyAsync(path, cancellationToken);

            foreach (var type in await container.GetTypesAsync(cancellationToken))
            {
                Console.WriteLine($"Type: {type}");
            }

            await container.StopAsync(cancellationToken: cancellationToken);

            container.Dispose();
        }
    }
}
