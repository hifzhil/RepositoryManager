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
    private const string DllName = "RepositoryNative";

    // -----------------------------------------------------------------
    // bool RM_Initialize()
    // -----------------------------------------------------------------
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool RM_Initialize();

    // -----------------------------------------------------------------
    // bool RM_Register(const char* itemName, const char* itemContent, int itemType)
    // -----------------------------------------------------------------
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool RM_Register(
        [MarshalAs(UnmanagedType.LPStr)] string itemName,
        [MarshalAs(UnmanagedType.LPStr)] string itemContent,
        int itemType);

    // -----------------------------------------------------------------
    // const char* RM_Retrieve(const char* itemName)
    // Returns a native heap pointer; caller MUST call RM_FreeString.
    // -----------------------------------------------------------------
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr RM_Retrieve(
        [MarshalAs(UnmanagedType.LPStr)] string itemName);

    // -----------------------------------------------------------------
    // int RM_GetType(const char* itemName)
    // -----------------------------------------------------------------
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RM_GetType(
        [MarshalAs(UnmanagedType.LPStr)] string itemName);

    // -----------------------------------------------------------------
    // bool RM_Deregister(const char* itemName)
    // -----------------------------------------------------------------
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool RM_Deregister(
        [MarshalAs(UnmanagedType.LPStr)] string itemName);

    // -----------------------------------------------------------------
    // bool RM_Contains(const char* itemName)
    // -----------------------------------------------------------------
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool RM_Contains(
        [MarshalAs(UnmanagedType.LPStr)] string itemName);

    // -----------------------------------------------------------------
    // void RM_FreeString(const char* str)
    // Releases a string previously allocated by the DLL (e.g. RM_Retrieve).
    // Must be called on the original IntPtr — never on a managed string.
    // -----------------------------------------------------------------
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RM_FreeString(IntPtr str);
}
