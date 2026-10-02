using BrowserAutomationMaster.Core.Messaging;
using Silk.NET.Windowing;
using static BrowserAutomationMaster.Core.Common.Constants;

namespace BrowserAutomationMaster.Core.Helpers 
{
    public static class ScreenHelper
    {
        public static (string monitorName, int? xSize, int? ySize) GetScreenSize() 
        {
            string monitorName = "Unknown";
            int? xSize = null;
            int? ySize = null;


            // 1. Create a hidden window configuration
            var options = WindowOptions.Default;
            options.IsVisible = false; // This prevents the popup
            options.WindowState = WindowState.Minimized;

            using var window = Window.Create(options);

            // 2. Initialize the windowing backend (Required for Monitor API)
            window.Initialize();

            // 3. Now you can safely access the monitor attached to this view
            var monitor = window.Monitor; 

            if (monitor == null) {
                return (monitorName, xSize, ySize);
            }

            try 
            {
                monitorName = monitor.Name;
                xSize = monitor.Bounds.Size.X;
                ySize = monitor.Bounds.Size.Y;
            }

            catch {
                Warning.Write(
                    string.Join(NLC, [
                        "A non fatal error occured while querying the bounds of your monitor."
                    ])
                );
            }

            finally {
                // Disposing of the window
                window.Close();
            }

            return (monitorName, xSize, ySize);

        }
    }
}
