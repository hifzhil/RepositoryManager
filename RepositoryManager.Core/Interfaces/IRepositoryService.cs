using RepositoryManager.Models;

namespace RepositoryManager.Core.Interfaces;

/// <summary>
/// Contract for all repository operations. Implementations can target the real
/// native DLL (via Interop) or a mock (for testing / design-time preview).
/// </summary>
public interface IRepositoryService
{
    /// <summary>Initialise the underlying repository backend.</summary>
    Task<bool> InitializeAsync();

    /// <summary>Upload (register) an item into the repository.</summary>
    Task<bool> RegisterAsync(string name, string content, ItemType type);

    /// <summary>Retrieve the raw content string of a named item.</summary>
    Task<string?> RetrieveAsync(string name);

    /// <summary>Delete (deregister) a named item from the repository.</summary>
    Task<bool> DeregisterAsync(string name);

    /// <summary>Check whether the repository contains a named item.</summary>
    Task<bool> ContainsAsync(string name);

    /// <summary>Return all items currently held in the repository.</summary>
    Task<IEnumerable<RepositoryItem>> GetAllItemsAsync();

    /// <summary>Total number of items in the repository.</summary>
    Task<int> GetItemCountAsync();
}
