using System;
using System.Runtime.InteropServices;

namespace BrowserAutomationMaster.Resources.NativeFileDialog
{
    public struct nfdpathset_t
    {
        public IntPtr buf;
        public IntPtr indices;
        public UIntPtr count;
    }

    public enum Nfdresult_t
    {
        NFD_ERROR,
        NFD_OKAY,
        NFD_CANCEL
    }

    public static partial class NativeFunctions
    {
        public const string LibraryName = "nfd";

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial Nfdresult_t NFD_OpenDialog(byte* filterList, byte* defaultPath, out IntPtr outPath);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial Nfdresult_t NFD_OpenDialogMultiple(byte* filterList, byte* defaultPath,
            nfdpathset_t* outPaths);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial Nfdresult_t NFD_SaveDialog(byte* filterList, byte* defaultPath, out IntPtr outPath);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial Nfdresult_t NFD_PickFolder(byte* defaultPath, out IntPtr outPath);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial byte* NFD_GetError();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial UIntPtr NFDPathSetGetCount(nfdpathset_t* pathSet);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial byte* NFDPathSetGetPath(nfdpathset_t* pathSet, UIntPtr index);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial void NFDPathSetFree(nfdpathset_t* pathSet);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial void NFD_Dummy();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial IntPtr NFD_Malloc(UIntPtr bytes);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial void NFD_Free(IntPtr ptr);
    }
    
    public static partial class NativeFunctions32
    {
        public const string LibraryName = "nfd_x86";

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial Nfdresult_t NFD_OpenDialog(byte* filterList, byte* defaultPath, out IntPtr outPath);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial Nfdresult_t NFD_OpenDialogMultiple(byte* filterList, byte* defaultPath,
            nfdpathset_t* outPaths);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial Nfdresult_t NFD_SaveDialog(byte* filterList, byte* defaultPath, out IntPtr outPath);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial Nfdresult_t NFD_PickFolder(byte* defaultPath, out IntPtr outPath);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial byte* NFD_GetError();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial UIntPtr NFD_PathSet_GetCount(nfdpathset_t* pathSet);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial byte* NFD_PathSet_GetPath(nfdpathset_t* pathSet, UIntPtr index);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial void NFD_PathSet_Free(nfdpathset_t* pathSet);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial void NFD_Dummy();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial IntPtr NFD_Malloc(UIntPtr bytes);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial void NFD_Free(IntPtr ptr);
    }
}