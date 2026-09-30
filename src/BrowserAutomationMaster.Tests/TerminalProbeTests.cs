using BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// Covers the terminal background-colour probe, which talks to /dev/tty. <br/>
    /// Source: Core/SystemInfo/OS/Unix/Linux/Functions.cs -> GetTerminalBackgroundColor
    /// </summary>
    /// <remarks>
    /// The probe writes an OSC 11 query to /dev/tty and reads the terminal's reply back from
    /// /dev/tty. /dev/tty is the controlling terminal shared with whatever launched the process, so
    /// probing it when stdin is redirected leaves the reply in the parent's input queue, where the
    /// next command typed at that terminal reads it. A test host has redirected stdin, so it must
    /// never trigger the probe.
    /// </remarks>
    public class TerminalProbeTests(ITestOutputHelper output)
    {
        [Fact]
        public void GetTerminalBackgroundColor_SkipsTheProbe_WhenStdinIsNotATerminal()
        {
            // Precondition: xunit gives the test host a redirected stdin, never a tty.
            Assert.True(Console.IsInputRedirected, "This test is only meaningful with a redirected stdin.");

            string? result = Functions.GetTerminalBackgroundColor();

            output.WriteLine($"Result: {result ?? "(null)"}");

            // null is the documented "unknown" signal; ThemeUtility.GetDefaultTheme falls back to
            // LightTheme. Anything else would mean the probe ran.
            Assert.Null(result);
        }
    }
}
