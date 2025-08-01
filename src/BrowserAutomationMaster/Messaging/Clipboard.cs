using BrowserAutomationMaster.Managers.Python;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BrowserAutomationMaster.Messaging
{
    public static partial class Clipboard
    {
        // This is the first win10 build, all versions before are not supported
        // https://en.wikipedia.org/wiki/Windows_10_version_history
        [SupportedOSPlatform("windows10.0.10240")]
        public static class Win
        {

            public unsafe static void SetText(string text)
            {
                OpenClipboard();
                PInvoke.EmptyClipboard();

                IntPtr hGlobal = default;
                try
                {
                    var bytes = (text.Length + 1) * 2;
                    hGlobal = Marshal.AllocHGlobal(bytes);

                    if (hGlobal == default)
                    {
                        ThrowWin32();
                    }

                    var target = PInvoke.GlobalLock((HGLOBAL)hGlobal);

                    if (target == default)
                    {
                        ThrowWin32();
                    }

                    try
                    {
                        Marshal.Copy(text.ToCharArray(), 0, (nint)target, text.Length);
                    }
                    finally
                    {
                        PInvoke.GlobalUnlock((HGLOBAL)target);
                    }

                    if (PInvoke.SetClipboardData(cfUnicodeText, (HANDLE)hGlobal) == default)
                    {
                        ThrowWin32();
                    }

                    hGlobal = default;
                }
                finally
                {
                    if (hGlobal != default)
                    {
                        Marshal.FreeHGlobal(hGlobal);
                    }

                    PInvoke.CloseClipboard();
                }
            }
            public static void OpenClipboard()
            {
                var num = 10;
                while (true)
                {
                    if (PInvoke.OpenClipboard(default))
                    {
                        break;
                    }

                    if (--num == 0)
                    {
                        ThrowWin32();
                    }

                    Thread.Sleep(100);
                }
            }

            const uint cfUnicodeText = 13;

            static void ThrowWin32()
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public static partial class OSX
        {
            public static void SetText(string text)
            {
                var nsString = objc_getClass("NSString");
                IntPtr str = default;
                IntPtr dataType = default;
                try
                {
                    str = objc_msgSend(objc_msgSend(nsString, sel_registerName("alloc")), sel_registerName("initWithUTF8String:"), text);
                    dataType = objc_msgSend(objc_msgSend(nsString, sel_registerName("alloc")), sel_registerName("initWithUTF8String:"), NSPasteboardTypeString);

                    var nsPasteboard = objc_getClass("NSPasteboard");
                    var generalPasteboard = objc_msgSend(nsPasteboard, sel_registerName("generalPasteboard"));

                    objc_msgSend(generalPasteboard, sel_registerName("clearContents"));
                    objc_msgSend(generalPasteboard, sel_registerName("setString:forType:"), str, dataType);
                }
                finally
                {
                    if (str != default)
                    {
                        objc_msgSend(str, sel_registerName("release"));
                    }

                    if (dataType != default)
                    {
                        objc_msgSend(dataType, sel_registerName("release"));
                    }
                }
            }

            [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit", StringMarshalling = StringMarshalling.Utf16)]
            private static partial IntPtr objc_getClass(string className);

            [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
            private static partial IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

            [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit", StringMarshalling = StringMarshalling.Utf16)]
            private static partial IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, string arg1);

            [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
            private static partial IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

            [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit", StringMarshalling = StringMarshalling.Utf16)]
            private static partial IntPtr sel_registerName(string selectorName);

            const string NSPasteboardTypeString = "public.utf8-plain-text";
        }

        public static class Linux
        {
            public static string Run(string commandLine)
            {
                var errorBuilder = new StringBuilder();
                var outputBuilder = new StringBuilder();
                var arguments = $"-c \"{commandLine}\"";
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                    }
                };
                process.Start();
                process.OutputDataReceived += (sender, args) => { outputBuilder.AppendLine(args.Data); };
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (sender, args) => { errorBuilder.AppendLine(args.Data); };
                process.BeginErrorReadLine();
                if (!process.WaitForExit(500))
                {
                    var timeoutError =
                        $@"Process timed out. Command line: bash {arguments}." +
                        $"Output: {outputBuilder}" +
                        $"Error: {errorBuilder}";

                    throw new Exception(timeoutError);
                }
                if (process.ExitCode == 0)
                {
                    return outputBuilder.ToString();
                }

                var error =
                    $@"Could not execute process. Command line: bash {arguments}." +
                    $"Output: {outputBuilder}" +
                    $"Error: {errorBuilder}";

                throw new Exception(error);
            }
            public static void SetText(string text)
            {
                var tempFileName = Path.GetTempFileName();
                File.WriteAllText(tempFileName, text);
                try
                {
                    Run($"cat {tempFileName} | xclip");
                }
                finally
                {
                    File.Delete(tempFileName);
                }
            }

            public static string GetText()
            {
                var tempFileName = Path.GetTempFileName();
                try
                {
                    Run($"xclip -o > {tempFileName}");
                    return File.ReadAllText(tempFileName);
                }
                finally
                {
                    File.Delete(tempFileName);
                }
            }
        }

        
    }

    public static class ClipboardHelper
    {
        private static readonly List<(Func<bool> PlatformCheck, Action<string> SetText)> platformFuncMap =
        [
            (IsWindows, SetWindowsText),
            (IsOSX, SetOSXText),
            (IsLinux, SetLinuxText)
        ];

        // These need to return the correct delegate types
        private static Func<bool> IsWindows => () => RuntimeManager.IsSupportedWindowsVersion();
        private static Func<bool> IsOSX => () => RuntimeManager.IsSupportedOSXVersion();
        private static Func<bool> IsLinux => () => OperatingSystem.IsLinux();

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        private static Action<string> SetWindowsText => text => Clipboard.Win.SetText(text);
        private static Action<string> SetOSXText => text => Clipboard.OSX.SetText(text);
        private static Action<string> SetLinuxText => text => Clipboard.Linux.SetText(text);

        public static bool TrySetText(string text)
        {
            foreach (var (platformCheck, setTextFunc) in platformFuncMap)
            {
                if (platformCheck())
                {
                    setTextFunc(text);
                    return true;
                }
            }
            return false;
        }
    }
}
