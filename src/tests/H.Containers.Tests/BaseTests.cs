using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using H.Containers.Tests.Utilities;

namespace H.Containers.Tests
{
    public static class BaseTests
    {
        public static async Task AsyncTest(TimeSpan timeout, Func<CancellationToken, Task> func)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            var cancellationToken = cancellationTokenSource.Token;

            await func(cancellationToken);
        }
        
        public static string Extract(this TempDirectory directory, string name)
        {
            var path = Path.Combine(directory.Folder, $"{name}.zip");
            var bytes = ResourcesUtilities.ReadFileAsBytes($"{name}.zip");
            File.WriteAllBytes(path, bytes);

            ZipFile.ExtractToDirectory(path, directory.Folder, true);

            return Path.Combine(directory.Folder, $"{name}.dll");
        }
        
        public static async Task LoadTestAsync(IContainer container, TempDirectory tempDirectory, CancellationToken cancellationToken = default)
        {
            var path = tempDirectory.Extract("H.Notifiers.RssNotifier");

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
