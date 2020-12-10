using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using H.IO;
using H.IO.Utilities;

namespace H.Containers
{
    internal static class TempDirectoryExtensions
    {
        #region Constants

        public const string ExeName = "H.Containers.Process.Application.exe";

        #endregion

        #region Methods

        public static string Unpack(this TempDirectory tempDirectory, ProcessRuntime runtime)
        {
            var name = runtime switch
            {
                ProcessRuntime.Net461 => "net4.6.1.zip",
                ProcessRuntime.Net48 => "net4.8.zip",
                ProcessRuntime.NetCore31 => "netcoreapp3.1.zip",
                ProcessRuntime.Net50 => "net5.0.zip",
                _ => throw new ArgumentException($"Runtime is not supported: {runtime}"),
            };
            var zipBytes = ResourcesUtilities.ReadFileAsBytes(name, Assembly.GetExecutingAssembly());

            var zipPath = Path.Combine(tempDirectory.Folder, name);
            File.WriteAllBytes(zipPath, zipBytes);

            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempDirectory.Folder);
            }
            finally
            {
                File.Delete(zipPath);
            }

            return Path.Combine(tempDirectory.Folder, ExeName);
        }

        #endregion
    }
}
