using System.Windows;
using Microsoft.Extensions.Configuration;
using RepositoryManager.Core.Interfaces;
using RepositoryManager.GUI.ViewModels;
using RepositoryManager.GUI.Views;
using RepositoryManager.Interop;
using RepositoryManager.Services;

namespace RepositoryManager.GUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        bool useNative = config.GetValue<bool>("UseNativeBackend");

        IRepositoryService service = useNative
            ? new NativeRepositoryService()
            : new MockRepositoryService();

        var viewModel = new MainViewModel(service);
        var window = new MainWindow(viewModel);

        MainWindow = window;
        window.Show();
    }
}