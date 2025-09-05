using System.Windows;
using System.Windows.Controls;
using App.UI.ViewModels;
using App.UI.Views;
using Prism.Ioc;

namespace App.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IContainerProvider _container;

    public MainWindow(IContainerProvider container)
    {
        InitializeComponent();
        _container = container ?? throw new ArgumentNullException(nameof(container));
        
        // Set up the main window view model
        var viewModel = _container.Resolve<MainWindowViewModel>();
        viewModel.NavigationRequested += OnNavigationRequested;
        DataContext = viewModel;
        
        // Navigate to home page initially
        NavigateToPage("HomePage");
    }
    
    private void OnNavigationRequested(object? sender, NavigationEventArgs e)
    {
        NavigateToPage(e.PageName);
    }
    
    private void NavigateToPage(string pageName)
    {
        UserControl? page = pageName switch
        {
            "HomePage" => CreatePageWithViewModel<HomePage, HomePageViewModel>(),
            "SettingsPage" => CreatePageWithViewModel<SettingsPage, SettingsPageViewModel>(),
            "LogsPage" => CreatePageWithViewModel<LogsPage, LogsPageViewModel>(),
            "AboutPage" => CreatePageWithViewModel<AboutPage, AboutPageViewModel>(),
            _ => null
        };
        
        if (page != null)
        {
            MainContentControl.Content = page;
        }
    }
    
    private T CreatePageWithViewModel<T, TViewModel>() 
        where T : UserControl, new() 
        where TViewModel : class
    {
        var page = new T();
        var viewModel = _container.Resolve<TViewModel>();
        page.DataContext = viewModel;
        return page;
    }
}

public class NavigationEventArgs : EventArgs
{
    public NavigationEventArgs(string pageName)
    {
        PageName = pageName;
    }
    
    public string PageName { get; }
}