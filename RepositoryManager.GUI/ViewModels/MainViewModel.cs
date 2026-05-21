using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using RepositoryManager.Core.Interfaces;
using RepositoryManager.GUI.Commands;
using RepositoryManager.Models;
using RepositoryManager.Services;

namespace RepositoryManager.GUI.ViewModels;

/// <summary>
/// Primary ViewModel driving MainWindow.
/// Owns all UI state, exposes commands, and delegates to IRepositoryService.
/// No business logic lives in the code-behind.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly IRepositoryService _service;

    // ---------------------------------------------------------------
    // Observable collections
    // ---------------------------------------------------------------
    public ObservableCollection<RepositoryItem> AllItems   { get; } = new();
    public ObservableCollection<RepositoryItem> FilteredItems { get; } = new();
    public ObservableCollection<string>         StatusLog   { get; } = new();

    // ---------------------------------------------------------------
    // Bindable properties
    // ---------------------------------------------------------------
    private RepositoryItem? _selectedItem;
    public RepositoryItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            OnPropertyChanged(nameof(HasSelection));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            ApplyFilter();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { SetProperty(ref _isBusy, value); CommandManager.InvalidateRequerySuggested(); }
    }

    private bool _isDarkTheme = true;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set { SetProperty(ref _isDarkTheme, value); OnThemeChanged(); }
    }

    private int _totalItems;
    public int TotalItems { get => _totalItems; private set => SetProperty(ref _totalItems, value); }

    private int _jsonCount;
    public int JsonCount { get => _jsonCount; private set => SetProperty(ref _jsonCount, value); }

    private int _xmlCount;
    public int XmlCount  { get => _xmlCount;  private set => SetProperty(ref _xmlCount, value); }

    public bool HasSelection => SelectedItem is not null;

    // ---------------------------------------------------------------
    // Commands
    // ---------------------------------------------------------------
    public ICommand UploadCommand   { get; }
    public ICommand DeleteCommand   { get; }
    public ICommand RefreshCommand  { get; }
    public ICommand ExportCommand   { get; }
    public ICommand ClearLogCommand { get; }

    // ---------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------
    public MainViewModel(IRepositoryService service)
    {
        _service = service;

        UploadCommand   = new AsyncRelayCommand(UploadAsync,   _ => !IsBusy);
        DeleteCommand   = new AsyncRelayCommand(DeleteAsync,   _ => !IsBusy && HasSelection);
        RefreshCommand  = new AsyncRelayCommand(RefreshAsync,  _ => !IsBusy);
        ExportCommand   = new RelayCommand(ExportSelected,     _ => HasSelection);
        ClearLogCommand = new RelayCommand(_ => StatusLog.Clear());

        _ = InitialiseAsync();
    }

    // ---------------------------------------------------------------
    // Initialisation
    // ---------------------------------------------------------------
    private async Task InitialiseAsync()
    {
        IsBusy = true;
        try
        {
            bool ok = await _service.InitializeAsync();
            Log(ok ? "Repository initialised." : "WARNING: Repository initialisation returned false.");
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            Log($"ERROR during initialisation: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    // ---------------------------------------------------------------
    // Upload
    // ---------------------------------------------------------------
    private async Task UploadAsync(object? _)
    {
        var dlg = new OpenFileDialog
        {
            Title            = "Select JSON or XML file",
            Filter           = "Supported files|*.json;*.xml|JSON files|*.json|XML files|*.xml",
            Multiselect      = true,
            CheckFileExists  = true
        };

        if (dlg.ShowDialog() != true) return;

        IsBusy = true;
        try
        {
            foreach (var path in dlg.FileNames)
                await UploadFileAsync(path);

            await LoadItemsAsync();
        }
        finally { IsBusy = false; }
    }

    /// <summary>Shared upload logic used by both the button and drag-drop.</summary>
    public async Task UploadFileAsync(string filePath)
    {
        string name = Path.GetFileName(filePath);
        string content;

        try { content = await File.ReadAllTextAsync(filePath); }
        catch (Exception ex) { Log($"ERROR reading '{name}': {ex.Message}"); return; }

        var (type, error) = ValidationService.ValidateContent(name, content);

        if (error is not null)
        {
            Log($"VALIDATION ERROR for '{name}': {error}");
            return;
        }

        bool ok = await _service.RegisterAsync(name, content, type);
        Log(ok ? $"Uploaded '{name}' [{type}]." : $"ERROR: Failed to register '{name}'.");

        if (ok) await LoadItemsAsync();
    }

    // ---------------------------------------------------------------
    // Delete
    // ---------------------------------------------------------------
    private async Task DeleteAsync(object? _)
    {
        if (SelectedItem is null) return;

        var result = MessageBox.Show(
            $"Delete '{SelectedItem.Name}' from the repository?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        string name = SelectedItem.Name;
        try
        {
            bool ok = await _service.DeregisterAsync(name);
            Log(ok ? $"Deleted '{name}'." : $"ERROR: Could not delete '{name}'.");
            SelectedItem = null;
            await LoadItemsAsync();
        }
        finally { IsBusy = false; }
    }

    // ---------------------------------------------------------------
    // Refresh
    // ---------------------------------------------------------------
    private async Task RefreshAsync(object? _)
    {
        IsBusy = true;
        try { await LoadItemsAsync(); Log("Repository refreshed."); }
        finally { IsBusy = false; }
    }

    // ---------------------------------------------------------------
    // Export
    // ---------------------------------------------------------------
    private void ExportSelected(object? _)
    {
        if (SelectedItem is null) return;

        var dlg = new SaveFileDialog
        {
            FileName         = SelectedItem.Name,
            Filter           = SelectedItem.ItemType == ItemType.Json
                                   ? "JSON files|*.json" : "XML files|*.xml",
            DefaultExt       = SelectedItem.ItemType == ItemType.Json ? ".json" : ".xml"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dlg.FileName, SelectedItem.Content);
            Log($"Exported '{SelectedItem.Name}' to '{dlg.FileName}'.");
        }
        catch (Exception ex)
        {
            Log($"ERROR exporting: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------
    // Internal helpers
    // ---------------------------------------------------------------
    private async Task LoadItemsAsync()
    {
        var items = (await _service.GetAllItemsAsync()).ToList();

        Application.Current.Dispatcher.Invoke(() =>
        {
            AllItems.Clear();
            foreach (var item in items) AllItems.Add(item);
            ApplyFilter();
            UpdateStats(items);
        });
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        var query = SearchText?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(query)
            ? AllItems
            : AllItems.Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var item in source) FilteredItems.Add(item);
    }

    private void UpdateStats(List<RepositoryItem> items)
    {
        TotalItems = items.Count;
        JsonCount  = items.Count(i => i.ItemType == ItemType.Json);
        XmlCount   = items.Count(i => i.ItemType == ItemType.Xml);
    }

    private void Log(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            if (StatusLog.Count > 200) StatusLog.RemoveAt(StatusLog.Count - 1);
        });
    }

    private void OnThemeChanged()
    {
        var uri = new Uri(IsDarkTheme
            ? "Themes/Dark.xaml"
            : "Themes/Light.xaml", UriKind.Relative);

        var dict = new ResourceDictionary { Source = uri };
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dict);
    }
}
