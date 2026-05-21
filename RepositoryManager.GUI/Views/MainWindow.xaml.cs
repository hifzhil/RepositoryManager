using System.Windows;
using System.Windows.Input;
using RepositoryManager.GUI.ViewModels;

namespace RepositoryManager.GUI.Views;

/// <summary>
/// MainWindow code-behind. Contains ONLY UI-lifecycle wiring and drag-drop
/// routing. All logic lives in MainViewModel.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    // ---------------------------------------------------------------
    // Drag-and-drop support
    // ---------------------------------------------------------------

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;

        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (files is null) return;

        foreach (var file in files)
            await _vm.UploadFileAsync(file);
    }
}
