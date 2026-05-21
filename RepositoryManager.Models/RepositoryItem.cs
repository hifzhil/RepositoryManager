using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RepositoryManager.Models;

/// <summary>
/// Represents the type of content stored in a repository item.
/// Must match the int values expected by the native DLL.
/// </summary>
public enum ItemType
{
    Unknown = 0,
    Json = 1,
    Xml = 2
}

/// <summary>
/// Domain model for a single repository item. Implements INotifyPropertyChanged
/// so WPF bindings update automatically when properties change.
/// </summary>
public class RepositoryItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _content = string.Empty;
    private ItemType _itemType;
    private DateTime _uploadedAt;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); }
    }

    public ItemType ItemType
    {
        get => _itemType;
        set { _itemType = value; OnPropertyChanged(); OnPropertyChanged(nameof(TypeLabel)); OnPropertyChanged(nameof(TypeBadgeColor)); }
    }

    public DateTime UploadedAt
    {
        get => _uploadedAt;
        set { _uploadedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(UploadedAtFormatted)); }
    }

    public string TypeLabel => ItemType switch
    {
        ItemType.Json => "JSON",
        ItemType.Xml  => "XML",
        _             => "???"
    };

    public string TypeBadgeColor => ItemType switch
    {
        ItemType.Json => "#F59E0B",
        ItemType.Xml  => "#3B82F6",
        _             => "#6B7280"
    };

    public string UploadedAtFormatted => UploadedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
