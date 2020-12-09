using System.IO;
using System.IO.Compression;
using System.Linq;
using H.Containers.Utilities;
using H.IO;

namespace H.Containers
{
    internal class ApplicationDirectory : TempDirectory
    {
        #region Constants

        public const string ExeName = "H.Containers.Process.Application.exe";

        #endregion

        #region Methods

        public string Unpack()
        {
            var names = ResourcesUtilities.GetResourcesNames().ToList();
            var firstName = names.First();
            var zipBytes = ResourcesUtilities.ReadFileAsBytes(firstName);

            var zipPath = Path.Combine(Folder, firstName);
            File.WriteAllBytes(zipPath, zipBytes);

            try
            {
                ZipFile.ExtractToDirectory(zipPath, Folder);
            }
            finally
            {
                File.Delete(zipPath);
            }

            return Path.Combine(Folder, ExeName);
        }

        #endregion
    }
}
