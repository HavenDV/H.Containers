using System;
using System.IO;
using System.Linq;
using H.Containers.Utilities;

namespace H.Containers
{
    public static class Application
    {
        private static string AppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
        private static string ApplicationPath => Directory.CreateDirectory(Path.Combine(AppDataPath, "H.Containers.Process.Application", "1.0.0")).FullName;

        public static string GetPathAndUnpackIfRequired()
        {
            if (!Directory.EnumerateFiles(ApplicationPath).Any())
            {
                Unpack();
            }

            return Path.Combine(ApplicationPath, "H.Containers.Process.Application.exe");
        }

        public static void Clear()
        {
            foreach (var path in Directory.EnumerateFiles(ApplicationPath))
            {
                File.Delete(path);
            }
        }

        private static void Unpack()
        {
            var names = ResourcesUtilities.GetResourcesNames().Select(GetName).ToList();
            var files = names.Select(name => ResourcesUtilities.ReadFileAsBytes(name)).ToList();

            foreach (var (name, bytes) in names.Zip(files, (a, b) => (a, b)))
            {
                var path = Path.Combine(ApplicationPath, name);

                File.WriteAllBytes(path, bytes);
            }
        }

        private static string GetName(string resourceName)
        {
            const string phrase = "Application.";

            var index = resourceName.IndexOf(phrase, StringComparison.Ordinal);
            if (index < 0)
            {
                throw new ArgumentException($"Invalid resourceName: {resourceName}");
            }

            return resourceName.Substring(index + phrase.Length);
        }
    }
}
