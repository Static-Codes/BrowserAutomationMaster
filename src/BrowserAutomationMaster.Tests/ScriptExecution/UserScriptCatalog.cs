using BrowserAutomationMaster.Core.Parsing;
using System.IO;

namespace BrowserAutomationMaster.Tests.ScriptExecution
{
    /// <summary>
    /// Discovers the .bamc scripts the <see cref="UserScriptExecutionTests"/> suite executes.
    /// </summary>
    /// <remarks>
    /// Discovery prefers the developer's real <c>userScripts</c> directory so that locally modified
    /// scripts are exercised. When that directory is empty (a fresh CI machine), the build-time copy
    /// of the repository <c>examples/</c> folder is used instead.
    /// </remarks>
    public static class UserScriptCatalog
    {
        /// <summary>
        /// Returns the absolute paths of every discoverable .bamc script, ordered ordinally so that
        /// theory case ordering is deterministic between runs.
        /// </summary>
        public static IReadOnlyList<string> Discover()
        {
            // This is invoked from MemberData, which xunit evaluates during *discovery*, before any
            // test has run. An uncaught exception here (for example, PlatformNotSupportedException
            // from DirectoryManager.GetAppDataDirectory()) aborts the entire test assembly rather
            // than failing a single test, so the whole body is guarded.
            try
            {
                // Parser.GetBAMCFiles() already applies the case-insensitive .bamc filter that BAMM
                // itself uses. Re-implementing that filter here would let the two drift apart.
                string[] discovered = Parser.GetBAMCFiles();

                if (discovered.Length == 0)
                {
                    string fallbackDirectory = Path.Combine(AppContext.BaseDirectory, "userScripts");

                    if (!Directory.Exists(fallbackDirectory))
                    {
                        return [];
                    }

                    discovered = [.. Directory.EnumerateFiles(
                        fallbackDirectory,
                        "*.bamc",
                        SearchOption.AllDirectories
                    )];
                }

                return [.. discovered
                    .Where(File.Exists)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => path, StringComparer.Ordinal)];
            }
            catch
            {
                return [];
            }
        }
    }
}
