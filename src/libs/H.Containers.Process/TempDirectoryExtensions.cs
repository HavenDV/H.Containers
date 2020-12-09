using System.IO;
using System.IO.Compression;
using System.Linq;
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

        public static string Unpack(this TempDirectory tempDirectory)
        {
            var names = ResourcesUtilities.GetResourcesNames().ToList();
            var firstName = names.First();
            var zipBytes = ResourcesUtilities.ReadFileAsBytes(firstName);

            var zipPath = Path.Combine(tempDirectory.Folder, firstName);
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
