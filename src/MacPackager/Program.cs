using System.Threading.Tasks;

namespace MacPackager
{
    internal class Program 
    {
        static void Main(string[] args)
        {
            // Console.WriteLine(await PlistManager.GetPlistContent());
            var bundleManager = new BundleManager();
            bundleManager.BuildBundle();
        }
    }
}
