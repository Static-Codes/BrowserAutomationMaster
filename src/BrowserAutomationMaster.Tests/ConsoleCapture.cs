using Spectre.Console;

using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// Diverts everything BAMM writes to the console into an in-memory buffer. <br/>
    /// Runs before any test, via <see cref="Runtime.CompilerServices.ModuleInitializerAttribute"/>.
    /// </summary>
    /// <remarks>
    /// The suites deliberately feed malformed input to BAMM to prove it is rejected, which makes the
    /// validation routines print their diagnostics. Those writes go to the process-global Console, so
    /// they land in the test report interleaved with the results and padded to the console width,
    /// making a passing run look like a wall of errors. Capturing them keeps the report readable;
    /// tests that need to assert on the text use <see cref="Captured"/>, and a genuinely unexpected
    /// message can still be surfaced through <see cref="ITestOutputHelper"/>.
    /// </remarks>
    internal static class ConsoleCapture
    {
        private const int MAX_CAPTURED_LENGTH = 64 * 1024;

        private static readonly StringBuilder buffer = new();

        private static TextWriter? originalOut;
        private static TextWriter? originalError;

        /// <summary>Everything written to the console so far, up to a bounded length.</summary>
        internal static string Captured
        {
            get
            {
                lock (buffer)
                {
                    return buffer.ToString();
                }
            }
        }

        [ModuleInitializer]
        internal static void Initialize()
        {
            originalOut = Console.Out;
            originalError = Console.Error;

            TextWriter capture = new CapturingTextWriter();

            Console.SetOut(capture);
            Console.SetError(capture);

            // Spectre.Console resolves its own writer at type init, so Console.SetOut alone is not
            // enough; AnsiConsole.Console has to be pointed at the capture as well.
            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Detect,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(capture)
            });
        }

        private static void Append(string text)
        {
            lock (buffer)
            {
                if (buffer.Length >= MAX_CAPTURED_LENGTH)
                {
                    return;
                }

                buffer.Append(text);
            }
        }

        /// <summary>A sink that keeps what BAMM prints instead of letting it reach the terminal.</summary>
        private sealed class CapturingTextWriter : TextWriter
        {
            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value) => Append(value.ToString());

            public override void Write(string? value)
            {
                if (value != null)
                {
                    Append(value);
                }
            }

            public override void WriteLine(string? value) => Append($"{value}{Environment.NewLine}");

            public override void WriteLine() => Append(Environment.NewLine);

            public override void Flush() { }
        }
    }
}
