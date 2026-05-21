using RepositoryManager.Core.Interfaces;
using RepositoryManager.Models;

namespace RepositoryManager.Services;

/// <summary>
/// In-memory mock that satisfies IRepositoryService without any native DLL.
/// Use this when the real DLL is unavailable (CI, design time, unit tests).
///
/// Switch between implementations in App.xaml.cs by changing which concrete
/// type is passed to MainViewModel.
/// </summary>
public sealed class MockRepositoryService : IRepositoryService
{
    private readonly Dictionary<string, RepositoryItem> _store = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public MockRepositoryService()
    {
        // Seed a few demo items so the UI is not empty on first launch.
        var seeds = new[]
        {
            new RepositoryItem
            {
                Name       = "config.json",
                Content    = "{\n  \"version\": \"1.0.0\",\n  \"debug\": false,\n  \"maxRetries\": 3\n}",
                ItemType   = ItemType.Json,
                UploadedAt = DateTime.UtcNow.AddHours(-2)
            },
            new RepositoryItem
            {
                Name       = "manifest.xml",
                Content    = "<?xml version=\"1.0\"?>\n<manifest>\n  <app id=\"repo-manager\" version=\"1.0\"/>\n</manifest>",
                ItemType   = ItemType.Xml,
                UploadedAt = DateTime.UtcNow.AddHours(-1)
            },
            new RepositoryItem
            {
                Name       = "users.json",
                Content    = "[\n  { \"id\": 1, \"name\": \"Alice\" },\n  { \"id\": 2, \"name\": \"Bob\" }\n]",
                ItemType   = ItemType.Json,
                UploadedAt = DateTime.UtcNow.AddMinutes(-30)
            }
        };

        foreach (var item in seeds)
            _store[item.Name] = item;
    }

    public Task<bool> InitializeAsync() => Task.FromResult(true);

    public async Task<bool> RegisterAsync(string name, string content, ItemType type)
    {
        await SimulatedDelayAsync();
        await _lock.WaitAsync();
        try
        {
            _store[name] = new RepositoryItem
            {
                Name       = name,
                Content    = content,
                ItemType   = type,
                UploadedAt = DateTime.UtcNow
            };
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<string?> RetrieveAsync(string name)
    {
        await SimulatedDelayAsync();
        await _lock.WaitAsync();
        try
        {
            return _store.TryGetValue(name, out var item) ? item.Content : null;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeregisterAsync(string name)
    {
        await SimulatedDelayAsync();
        await _lock.WaitAsync();
        try { return _store.Remove(name); }
        finally { _lock.Release(); }
    }

    public async Task<bool> ContainsAsync(string name)
    {
        await SimulatedDelayAsync();
        await _lock.WaitAsync();
        try { return _store.ContainsKey(name); }
        finally { _lock.Release(); }
    }

    public async Task<IEnumerable<RepositoryItem>> GetAllItemsAsync()
    {
        await _lock.WaitAsync();
        try { return _store.Values.OrderByDescending(x => x.UploadedAt).ToList(); }
        finally { _lock.Release(); }
    }

    public async Task<int> GetItemCountAsync()
    {
        await _lock.WaitAsync();
        try { return _store.Count; }
        finally { _lock.Release(); }
    }

    private static Task SimulatedDelayAsync() => Task.Delay(50);
}
