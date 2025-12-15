using System.Threading.Tasks;

namespace MacPackager
{
    internal class Program 
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(await PlistManager.GetPlistContent());
        }
    }
}
