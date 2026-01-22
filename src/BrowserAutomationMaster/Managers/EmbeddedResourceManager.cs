using System.Reflection;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers
{
    public class EmbeddedResourceManager 
    {
        private static readonly Assembly assembly = Assembly.GetExecutingAssembly();

        public static Stream GetEmbeddedResource(string resourceName, string resourcePattern) 
        {
            // var resourcePattern = "*MacPackager*.*AppIcon*.icns";
            Stream? resourceStream = null;

            try
            {
                resourceStream = assembly.GetManifestResourceStream(resourcePattern);

                if (resourceStream == null) 
                {
                    WriteAndExit
                    (
                        string.Join(NLC, [
                            $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                            "Error Log:",
                            "resourceStream returned null"
                        ]), 
                        status: 1
                    );
                }
            }
            catch (Exception ex)
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                        $"Error Log:{NLC}{ex.StackTrace ?? ex.Message}"
                    ]), 
                    status: 1
                );
            }

            return resourceStream;
        }

    }
}