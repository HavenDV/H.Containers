using System.Threading.Tasks;

namespace H.Containers
{
    internal static class Program
    {
        private static async Task Main(string[] arguments)
        {
            await ChildProgram.Main(arguments, true);
        }
    }
}
