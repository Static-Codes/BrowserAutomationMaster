using BrowserAutomationMaster.Core.Messaging;
using Silk.NET.Windowing;
using static BrowserAutomationMaster.Core.Common.Constants;

namespace BrowserAutomationMaster.Core.Helpers;

public static class ScreenHelper
{
    public static (string monitorName, int? xSize, int? ySize) GetScreenSize() 
    {
        string monitorName = "Unknown";
        int? xSize = null;
        int? ySize = null;


        // Creating a hidden window config
        var options = WindowOptions.Default;
        
        options.IsVisible = false; // This prevents the popup
        options.WindowState = WindowState.Minimized;

        using var window = Window.Create(options);

        // Initializing the windowing backend (Required for Monitor API)
        window.Initialize();

        // 3. Now you can safely access the monitor attached to this view
        var monitor = window.Monitor; 

        if (monitor == null) { return (monitorName, xSize, ySize); }

        try 
        {
            monitorName = monitor.Name;
            xSize = monitor.Bounds.Size.X;
            ySize = monitor.Bounds.Size.Y;
        }

        catch (Exception ex) 
        {
            Warning.Write(string.Join(Environment.NewLine, [
                "A non fatal error occured while querying the bounds of your monitor.",
                $"Error: {ex.Message}"
            ]));
        }

        // Disposing of the window
        finally { window.Close(); }

        return (monitorName, xSize, ySize);

    }
}