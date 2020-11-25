using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using H.Containers.Utilities;

namespace H.Containers
{
    /// <summary>
    /// 
    /// </summary>
    public static class Application
    {
        private static string AppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
        private static string ApplicationPath => Directory.CreateDirectory(Path.Combine(AppDataPath, "H.Containers.Process.Application", "1.0.0")).FullName;
        private static IEnumerable<string> Files => Directory.EnumerateFiles(ApplicationPath, "*.*", SearchOption.AllDirectories);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetPathAndUnpackIfRequired()
        {
            if (!Files.Any())
            {
                Unpack();
            }

            return Path.Combine(ApplicationPath, "H.Containers.Process.Application.exe");
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Clear()
        {
            foreach (var path in Files)
            {
                File.Delete(path);
            }
        }

        private static void Unpack()
        {
            var names = ResourcesUtilities.GetResourcesNames().ToList();
            var firstName = names.First();
            var zipBytes = ResourcesUtilities.ReadFileAsBytes(firstName);

            var zipPath = Path.Combine(ApplicationPath, firstName);
            File.WriteAllBytes(zipPath, zipBytes);

            try
            {
                ZipFile.ExtractToDirectory(zipPath, ApplicationPath);
            }
            finally
            {
                File.Delete(zipPath);
            }
        }
    }
}
