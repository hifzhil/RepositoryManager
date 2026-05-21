using System.Windows;
using RepositoryManager.GUI.ViewModels;
using RepositoryManager.GUI.Views;
using RepositoryManager.Services;

// To use the REAL native DLL instead of the mock, swap the commented lines below.
// using RepositoryManager.Interop;

namespace RepositoryManager.GUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // -------------------------------------------------------
        // Composition root — wire dependencies here.
        // Swap MockRepositoryService for NativeRepositoryService
        // once the native DLL is in place.
        // -------------------------------------------------------
        var service = new MockRepositoryService();
        // var service = new NativeRepositoryService();

        var vm     = new MainViewModel(service);
        var window = new MainWindow(vm);

        MainWindow = window;
        window.Show();
    }
}
