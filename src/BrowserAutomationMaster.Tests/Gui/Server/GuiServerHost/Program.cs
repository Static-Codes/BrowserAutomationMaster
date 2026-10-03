using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Python;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;

// Aliased rather than referenced as Server: the test suite's own .Gui.Server namespace would otherwise
// shadow Core.GUI.Server at every use site, and a using-alias does not win that lookup.
using GuiServer = BrowserAutomationMaster.Core.GUI.Server;

namespace BrowserAutomationMaster.Tests.Gui.ServerHost
{
    /// <summary>
    /// Holds the GUI's HttpListener open so the Tier B tests can talk to it over a real socket. <br/>
    /// Source: Core/GUI/Server.cs (StartServer)
    /// </summary>
    /// <remarks>
    /// Deliberately minimal. Everything the <c>gui</c> CLI argument does beyond the listener is left
    /// out: the Photino window needs a display, and the single-instance check, the daemon, and the
    /// browser launch are not what these tests are about. <br/>
    /// This process is expected to be killed rather than to exit on its own, so there is no graceful
    /// shutdown path here — <c>/terminate</c> is how a test asks it to stop, and the harness kills it if
    /// that does not happen.
    /// </remarks>
    internal static class Program
    {
        /// <summary>
        /// Starts the listener on the port given as the first argument, or on the server's default.
        /// </summary>
        /// <remarks>
        /// Two pieces of setup the real program does in ProgramFunctions.InitializeAsync are reproduced
        /// here, because Server.StartServer needs both and its failure mode is to exit rather than
        /// complain: <br/>
        /// SetPlatform, or DirectoryManager.AppDataDirectory throws PlatformNotSupportedException. <br/>
        /// SetMemoryInfo, or StartServer sees a null MemoryInfo and refuses to start on a machine that
        /// in fact has 128GB free — its check is "did I manage to read the memory", not "is there
        /// enough memory".
        /// </remarks>
        internal static async Task<int> Main(string[] args)
        {
            PlatformManager.SetPlatform(GlobalUserInfo);

            await Runtime.SetMemoryInfo();

            string port = args.Length > 0 && args[0].Length > 0 ? args[0] : GuiServer.DEFAULT_PORT;

            // Announced on stdout so the harness can tell "started and listening" from "still
            // extracting the GUI". Without it a readiness probe can only guess from a timeout.
            Console.WriteLine($"GuiServerHost listening on port {port}");

            await GuiServer.StartServer(port);

            return 0;
        }
    }
}
