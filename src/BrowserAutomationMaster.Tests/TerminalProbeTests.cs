using BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// Covers the terminal background-color probe, which talks to /dev/tty. <br/>
    /// Source: Core/SystemInfo/OS/Unix/Linux/Functions.cs -> GetTerminalBackgroundColor
    /// </summary>
    public class TerminalProbeTests(ITestOutputHelper output)
    {
        [SkippableFact]
        public void GetTerminalBackgroundColor_SkipsTheProbe_WhenStdinIsNotATerminal()
        {
            // The probe writes an OSC 11 query to /dev/tty and reads the reply back from /dev/tty.
            // /dev/tty is the controlling terminal shared with whatever launched the process, so
            // probing it with a redirected stdin leaves the reply in the parent's input queue, where
            // the next command typed at that terminal reads it.
            // Whether stdin is redirected is a property of how the suite was launched and not of the
            // code under test, so an unmet precondition skips instead of failing.
            Skip.IfNot(Console.IsInputRedirected,
                "Skipped: this host has a tty on stdin. The /dev/tty probe is not safe to drive from a test here, so the skip path is not exercised.");

            string? result = Functions.GetTerminalBackgroundColor();

            output.WriteLine($"Result: {result ?? "(null)"}");

            // A null value signifies an "unknown" signal.
            // ThemeUtility.GetDefaultTheme falls back to LightTheme. 
            // If any other value is returned, that means the probe ran successfully.
            Assert.Null(result);
        }
    }
}
