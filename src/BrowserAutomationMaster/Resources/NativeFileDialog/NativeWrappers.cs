using System.Diagnostics;
using System.Text;
using BrowserAutomationMaster.Resources.NativeFileDialog;

namespace BrowserAutomationMaster.Resources.NativeFileDialog
{
    public static class Dialog
    {
        private static readonly Encoder utf8encoder = Encoding.UTF8.GetEncoder();

        private static readonly bool need32bit = Is32BitWindowsOnNetFramework();
        
        private static bool Is32BitWindowsOnNetFramework()
        {
            try
            {
                // we call a function that does nothing just to test if we can load it properly
                NativeFunctions.NFD_Dummy();
                return false;
            }
            catch
            {
                // a call to a default library failed, let's attempt the other one
                try
                {
                    NativeFunctions32.NFD_Dummy();
                    return true;
                }
                catch
                {
                    // both of them failed so we may as well default to the default one for predictability
                    return false;
                }
            }
        }

        private static unsafe byte[] ToUtf8(string s)
        {
            var byteCount = Encoding.UTF8.GetByteCount(s);
            var bytes = new byte[byteCount + 1];
            fixed (byte* o = bytes)
            fixed (char* input = s)
            {
                utf8encoder.Convert(input, s.Length, o, bytes.Length, true, out _, out var _,
                    out var completed);
                Debug.Assert(completed);
            }

            return bytes;
        }

        private static unsafe int GetNullTerminatedStringLength(byte* nullTerminatedString)
        {
            int count = 0;
            var ptr = nullTerminatedString;
            while (*ptr != 0)
            {
                ptr++;
                count++;
            }

            return count;
        }

        private static unsafe string FromUtf8(byte* nullTerminatedString)
        {
            return Encoding.UTF8.GetString(nullTerminatedString, GetNullTerminatedStringLength(nullTerminatedString));
        }

        public static unsafe DialogResult FileOpen(string? filterList = null, string? defaultPath = null)
        {
            fixed (byte* filterListNts = filterList != null ? ToUtf8(filterList) : null)
            fixed (byte* defaultPathNts = defaultPath != null ? ToUtf8(defaultPath) : null)
            {
                string? path = null;
                string? errorMessage = null;
                var result = need32bit
                    ? NativeFunctions32.NFD_OpenDialog(filterListNts, defaultPathNts, out nint outPathIntPtr)
                    : NativeFunctions.NFD_OpenDialog(filterListNts, defaultPathNts, out outPathIntPtr);

                if (result == Nfdresult_t.NFD_ERROR)
                {
                    errorMessage = FromUtf8(NativeFunctions.NFD_GetError());
                }
                else if (result == Nfdresult_t.NFD_OKAY)
                {
                    var outPathNts = (byte*)outPathIntPtr.ToPointer();
                    path = FromUtf8(outPathNts);
                    NativeFunctions.NFD_Free(outPathIntPtr);
                }

                return new DialogResult(result, path, null, errorMessage);
            }
        }

        public static unsafe DialogResult FileSave(string? filterList = null, string? defaultPath = null)
        {
            fixed (byte* filterListNts = filterList != null ? ToUtf8(filterList) : null)
            fixed (byte* defaultPathNts = defaultPath != null ? ToUtf8(defaultPath) : null)
            {
                string? path = null;
                string? errorMessage = null;
                nint outPathIntPtr;
                var result = need32bit 
                    ? NativeFunctions32.NFD_SaveDialog(filterListNts, defaultPathNts, out outPathIntPtr) 
                    : NativeFunctions.NFD_SaveDialog(filterListNts, defaultPathNts, out outPathIntPtr);
                if (result == Nfdresult_t.NFD_ERROR)
                {
                    errorMessage = FromUtf8(NativeFunctions.NFD_GetError());
                }
                else if (result == Nfdresult_t.NFD_OKAY)
                {
                    var outPathNts = (byte*)outPathIntPtr.ToPointer();
                    path = FromUtf8(outPathNts);
                    NativeFunctions.NFD_Free(outPathIntPtr);
                }

                return new DialogResult(result, path, null, errorMessage);
            }
        }

        public static unsafe DialogResult FolderPicker(string? defaultPath = null)
        {
            fixed (byte* defaultPathNts = defaultPath != null ? ToUtf8(defaultPath) : null)
            {
                string? path = null;
                string? errorMessage = null;
                var result = need32bit
                    ? NativeFunctions32.NFD_PickFolder(defaultPathNts, out nint outPathIntPtr)
                    : NativeFunctions.NFD_PickFolder(defaultPathNts, out outPathIntPtr);

                if (result == Nfdresult_t.NFD_ERROR)
                {
                    errorMessage = FromUtf8(NativeFunctions.NFD_GetError());
                }
                else if (result == Nfdresult_t.NFD_OKAY)
                {
                    var outPathNts = (byte*)outPathIntPtr.ToPointer();
                    path = FromUtf8(outPathNts);
                    NativeFunctions.NFD_Free(outPathIntPtr);
                }

                return new DialogResult(result, path, null, errorMessage);
            }
        }

        public static unsafe DialogResult FileOpenMultiple(string? filterList = null, string? defaultPath = null)
        {
            fixed (byte* filterListNts = filterList != null ? ToUtf8(filterList) : null)
            fixed (byte* defaultPathNts = defaultPath != null ? ToUtf8(defaultPath) : null)
            {
                List<string>? paths = null;
                string? errorMessage = null;
                nfdpathset_t pathSet;
                var result = need32bit
                    ? NativeFunctions32.NFD_OpenDialogMultiple(filterListNts, defaultPathNts, &pathSet)
                    : NativeFunctions.NFD_OpenDialogMultiple(filterListNts, defaultPathNts, &pathSet);
                if (result == Nfdresult_t.NFD_ERROR)
                {
                    errorMessage = FromUtf8(NativeFunctions.NFD_GetError());
                }
                else if (result == Nfdresult_t.NFD_OKAY)
                {
                    var pathCount = (int)NativeFunctions.NFDPathSetGetCount(&pathSet).ToUInt32();
                    paths = new List<string>(pathCount);
                    for (int i = 0; i < pathCount; i++)
                    {
                        paths.Add(FromUtf8(NativeFunctions.NFDPathSetGetPath(&pathSet, new nuint((uint)i))));
                    }

                    NativeFunctions.NFDPathSetFree(&pathSet);
                }

                return new DialogResult(result, null, paths, errorMessage);
            }
        }
    }

    public class DialogResult
    {
        private readonly Nfdresult_t result;

        public string? Path { get; }

        public IReadOnlyList<string>? Paths { get; }

        public bool IsError => result == Nfdresult_t.NFD_ERROR;

        public string? ErrorMessage { get; }

        public bool IsCancelled => result == Nfdresult_t.NFD_CANCEL;

        public bool IsOk => result == Nfdresult_t.NFD_OKAY;

        internal DialogResult(Nfdresult_t result, string? path, IReadOnlyList<string>? paths, string? errorMessage)
        {
            this.result = result;
            Path = path;
            Paths = paths;
            ErrorMessage = errorMessage;
        }
    }
}