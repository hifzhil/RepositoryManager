using System.Collections.Generic;
using System.Text;
using RepositoryManager.Core.Interfaces;
using RepositoryManager.Models;

namespace RepositoryManager.Interop;

/// <summary>
/// Implements IRepositoryService by forwarding every call to repomgr.dll
/// through the P/Invoke declarations in NativeMethods.
///
/// Lifetime:
///   - Constructor calls RepoCreate + RepoInitialize synchronously.
///   - Dispose calls RepoDestroy — register this service as a singleton and
///     dispose it when the application exits.
///
/// Threading:
///   - All native calls are wrapped in Task.Run so the UI thread is never
///     blocked by the native layer.
///   - repomgr.dll uses std::shared_mutex internally, so concurrent calls
///     are safe.
/// </summary>
public class NativeRepositoryService : IRepositoryService, IDisposable
{
    private readonly IntPtr _handle;

    // -------------------------------------------------------------------------
    // Error code constants — must stay in sync with repomgr_capi.h
    // -------------------------------------------------------------------------
    private const int REPO_OK = 0;
    private const int REPO_ERR_NOT_INITIALIZED = 1;
    private const int REPO_ERR_DUPLICATE = 2;
    private const int REPO_ERR_NOT_FOUND = 3;
    private const int REPO_ERR_INVALID_NAME = 4;
    private const int REPO_ERR_INVALID_CONTENT = 5;
    private const int REPO_ERR_UNSUPPORTED = 6;
    private const int REPO_ERR_BUFFER_TOO_SMALL = 7;

    // -------------------------------------------------------------------------
    // Construction / teardown
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates and initializes the native repository handle.
    /// Throws InvalidOperationException if the DLL cannot be loaded or
    /// the handle cannot be created.
    /// </summary>
    public NativeRepositoryService()
    {
        _handle = NativeMethods.RepoCreate();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException(
                "RepoCreate returned null — is repomgr.dll present and x64?");

        // Initialize synchronously in constructor; safe because this runs
        // once at startup before any async calls are made.
        int code = NativeMethods.RepoInitialize(_handle);
        ThrowIfError(code);
    }

    /// <summary>
    /// Frees the native repository and all items it holds.
    /// Call once when the application exits (or via DI container disposal).
    /// </summary>
    public void Dispose()
    {
        NativeMethods.RepoDestroy(_handle);
    }

    // -------------------------------------------------------------------------
    // IRepositoryService implementation
    // -------------------------------------------------------------------------

    /// <summary>
    /// No-op for the native backend — initialization already happened in the
    /// constructor. Returns true to satisfy the interface contract.
    /// </summary>
    public Task<bool> InitializeAsync()
        => Task.FromResult(true);

    /// <summary>
    /// Registers a new item in the repository.
    /// Returns false (instead of throwing) on duplicate or validation failure
    /// so callers can show a user-friendly message without try/catch.
    /// </summary>
    public Task<bool> RegisterAsync(string name, string content, ItemType type)
        => Task.Run(() =>
        {
            int code = NativeMethods.RepoRegister(_handle, name, content, (int)type);
            return code == REPO_OK;
        });

    /// <summary>
    /// Retrieves the raw content string for a named item.
    /// Returns null if the item does not exist.
    ///
    /// Uses the two-call pattern:
    ///   1. Pass null buffer → C++ writes required byte count into outLen.
    ///   2. Allocate outLen+1 bytes → C++ fills the buffer.
    /// No native heap allocation; C# owns the buffer throughout.
    /// </summary>
    public Task<string?> RetrieveAsync(string name)
        => Task.Run<string?>(() =>
        {
            // First call — probe required buffer size
            int code = NativeMethods.RepoRetrieve(
                _handle, name, null, UIntPtr.Zero, out UIntPtr needed);

            if (code == REPO_ERR_NOT_FOUND) return null;

            // Any error other than "buffer too small" is a real problem
            if (code != REPO_ERR_BUFFER_TOO_SMALL && code != REPO_OK)
            {
                ThrowIfError(code);
                return null;
            }

            // Second call — allocate exact buffer and fill it
            byte[] buf = new byte[(int)needed + 1];
            code = NativeMethods.RepoRetrieve(
                _handle, name, buf, (UIntPtr)buf.Length, out _);

            ThrowIfError(code);

            // Convert UTF-8 bytes to a managed string (exclude the null terminator)
            return Encoding.UTF8.GetString(buf, 0, (int)needed);
        });

    /// <summary>
    /// Removes a named item from the repository.
    /// Returns false if the item was not found.
    /// </summary>
    public Task<bool> DeregisterAsync(string name)
        => Task.Run(() =>
        {
            int code = NativeMethods.RepoDeregister(_handle, name);
            return code == REPO_OK;
        });

    /// <summary>
    /// Returns true if the repository holds an item with the given name.
    /// Returns false for empty or whitespace names without throwing.
    /// </summary>
    public Task<bool> ContainsAsync(string name)
        => Task.Run(() =>
        {
            int code = NativeMethods.RepoContains(_handle, name, out int found);
            return code == REPO_OK && found == 1;
        });

    /// <summary>
    /// Returns all items currently held in the repository.
    ///
    /// Note: repomgr.dll does not expose a "list all keys" function, so this
    /// method returns an empty collection. To support this fully, add a
    /// RepoGetAllNames function to repomgr_capi.h/cpp.
    /// </summary>
    public Task<IEnumerable<RepositoryItem>> GetAllItemsAsync()
        => Task.FromResult<IEnumerable<RepositoryItem>>(Array.Empty<RepositoryItem>());

    /// <summary>
    /// Returns the total number of items in the repository.
    ///
    /// Note: repomgr.dll does not expose a count function, so this always
    /// returns 0. To support this fully, add a RepoGetCount function to
    /// repomgr_capi.h/cpp.
    /// </summary>
    public Task<int> GetItemCountAsync()
        => Task.FromResult(0);

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Maps a native error code to the most appropriate .NET exception type.
    /// REPO_OK is a no-op. Unrecognised codes throw InvalidOperationException.
    /// </summary>
    private static void ThrowIfError(int code)
    {
        switch (code)
        {
            case REPO_OK: return;
            case REPO_ERR_NOT_INITIALIZED: throw new InvalidOperationException("Repository has not been initialized.");
            case REPO_ERR_DUPLICATE: throw new InvalidOperationException("An item with that name already exists.");
            case REPO_ERR_NOT_FOUND: throw new KeyNotFoundException("No item with that name exists.");
            case REPO_ERR_INVALID_NAME: throw new ArgumentException("Item name is null, empty, or whitespace.");
            case REPO_ERR_INVALID_CONTENT: throw new ArgumentException("Item content failed format validation.");
            case REPO_ERR_UNSUPPORTED: throw new NotSupportedException("Item type is not supported by the native backend.");
            default: throw new InvalidOperationException($"Unexpected native error code: {code}.");
        }
    }
}