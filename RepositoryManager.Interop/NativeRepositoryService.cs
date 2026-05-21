using System.Runtime.InteropServices;
using RepositoryManager.Core.Interfaces;
using RepositoryManager.Models;

namespace RepositoryManager.Interop;

/// <summary>
/// IRepositoryService implementation that delegates every operation to the
/// native C++ DLL through the NativeMethods P/Invoke wrapper.
///
/// Thread safety: all public methods are async and run on the thread-pool so
/// the WPF UI thread is never blocked by native calls.
/// </summary>
public sealed class NativeRepositoryService : IRepositoryService
{
    // In-memory mirror so we can return a full list without querying each
    // item name from native (the DLL has no enumerate API).
    private readonly Dictionary<string, RepositoryItem> _mirror = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<bool> InitializeAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                return NativeMethods.RM_Initialize();
            }
            catch (DllNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "RepositoryNative.dll was not found. " +
                    "Place the compiled DLL in the application output directory.", ex);
            }
        });
    }

    public async Task<bool> RegisterAsync(string name, string content, ItemType type)
    {
        bool ok = await Task.Run(() =>
            NativeMethods.RM_Register(name, content, (int)type));

        if (ok)
        {
            await _lock.WaitAsync();
            try
            {
                _mirror[name] = new RepositoryItem
                {
                    Name       = name,
                    Content    = content,
                    ItemType   = type,
                    UploadedAt = DateTime.UtcNow
                };
            }
            finally { _lock.Release(); }
        }
        return ok;
    }

    public async Task<string?> RetrieveAsync(string name)
    {
        return await Task.Run(() =>
        {
            // RM_Retrieve returns a native-heap pointer; marshal then free.
            IntPtr ptr = NativeMethods.RM_Retrieve(name);
            if (ptr == IntPtr.Zero) return null;

            try
            {
                return Marshal.PtrToStringAnsi(ptr);
            }
            finally
            {
                // Always free even if PtrToStringAnsi throws.
                NativeMethods.RM_FreeString(ptr);
            }
        });
    }

    public async Task<bool> DeregisterAsync(string name)
    {
        bool ok = await Task.Run(() => NativeMethods.RM_Deregister(name));

        if (ok)
        {
            await _lock.WaitAsync();
            try { _mirror.Remove(name); }
            finally { _lock.Release(); }
        }
        return ok;
    }

    public Task<bool> ContainsAsync(string name) =>
        Task.Run(() => NativeMethods.RM_Contains(name));

    public async Task<IEnumerable<RepositoryItem>> GetAllItemsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _mirror.Values.ToList();
        }
        finally { _lock.Release(); }
    }

    public async Task<int> GetItemCountAsync()
    {
        await _lock.WaitAsync();
        try { return _mirror.Count; }
        finally { _lock.Release(); }
    }
}
