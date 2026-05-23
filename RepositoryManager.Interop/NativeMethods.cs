using System.Runtime.InteropServices;

namespace RepositoryManager.Interop;

/// <summary>
/// Raw P/Invoke declarations for every function exported by RepositoryNative.dll.
///
/// Key marshalling decisions:
///   - Strings flowing INTO the DLL use [MarshalAs(UnmanagedType.LPStr)] which
///     pins a UTF-8 / ANSI byte pointer — matching a C  `const char*` parameter.
///   - Strings flowing OUT of the DLL (RM_Retrieve) are received as IntPtr so we
///     can call RM_FreeString on the same pointer. Never let the runtime free a
///     native heap allocation.
///   - The DLL must be compiled for x64 and placed beside the EXE (or in `native/`
///     and copied via the .csproj ItemGroup).
/// </summary>
internal static class NativeMethods
{
    private const string Dll = "repomgr";

    // -----------------------------------------------------------------
    // RepoHandle* RepoCreate()
    // Allocates a new repository instance on the native heap.
    // Returns IntPtr.Zero on allocation failure.
    // Caller MUST call RepoDestroy when done to avoid memory leaks.
    // -----------------------------------------------------------------
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr RepoCreate();

    // -----------------------------------------------------------------
    // void RepoDestroy(RepoHandle* h)
    // Frees the repository instance and all items it holds.
    // Must be called exactly once per RepoCreate call.
    // -----------------------------------------------------------------
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RepoDestroy(IntPtr h);

    // -----------------------------------------------------------------
    // int32_t RepoInitialize(RepoHandle* h)
    // Must be called exactly once after RepoCreate before any other call.
    // Returns REPO_ERR_DUPLICATE if called more than once.
    // -----------------------------------------------------------------
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RepoInitialize(IntPtr h);

    // -----------------------------------------------------------------
    // int32_t RepoRegister(RepoHandle* h,
    //                      const char* name,
    //                      const char* content,
    //                      int32_t itemType)
    // itemType: 1 = JSON, 2 = XML.
    // Returns REPO_ERR_DUPLICATE if name already exists.
    // Returns REPO_ERR_INVALID_CONTENT if content fails format validation.
    // -----------------------------------------------------------------
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RepoRegister(
        IntPtr h,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        [MarshalAs(UnmanagedType.LPStr)] string content,
        int itemType);

    // -----------------------------------------------------------------
    // int32_t RepoRetrieve(RepoHandle* h,
    //                      const char* name,
    //                      char*    outBuf,
    //                      size_t   bufLen,
    //                      size_t*  outLen)
    // Two-call pattern:
    //   1st call: pass null/0 buffer → C++ writes required size into outLen
    //             → returns REPO_ERR_BUFFER_TOO_SMALL
    //   2nd call: allocate outLen+1 bytes, pass as outBuf → C++ fills it
    //             → returns REPO_OK
    // No native heap allocation — C# owns the buffer throughout.
    // -----------------------------------------------------------------
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RepoRetrieve(
        IntPtr h,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        byte[] outBuf,
        UIntPtr bufLen,
        out UIntPtr outLen);

    // -----------------------------------------------------------------
    // int32_t RepoGetType(RepoHandle* h,
    //                     const char* name,
    //                     int32_t*    outType)
    // Writes 1 (JSON) or 2 (XML) into outType on success.
    // Returns REPO_ERR_NOT_FOUND if name does not exist.
    // -----------------------------------------------------------------
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RepoGetType(
        IntPtr h,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        out int outType);

    // -----------------------------------------------------------------
    // int32_t RepoDeregister(RepoHandle* h, const char* name)
    // Removes the item permanently from the repository.
    // Returns REPO_ERR_NOT_FOUND if name does not exist.
    // -----------------------------------------------------------------
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RepoDeregister(
        IntPtr h,
        [MarshalAs(UnmanagedType.LPStr)] string name);

    // -----------------------------------------------------------------
    // int32_t RepoContains(RepoHandle* h,
    //                      const char* name,
    //                      int32_t*    outFound)
    // Writes 1 (exists) or 0 (not found) into outFound on success.
    // Does NOT throw for empty or whitespace names — outFound = 0.
    // -----------------------------------------------------------------
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RepoContains(
        IntPtr h,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        out int outFound);
}
