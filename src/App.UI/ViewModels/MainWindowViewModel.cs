using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using MaterialDesignThemes.Wpf;

namespace App.UI.ViewModels;

/// <summary>
/// Main window view model handling navigation and theme management.
/// </summary>
public class MainWindowViewModel : BindableBase
{
    private bool _isDarkTheme;

    public MainWindowViewModel()
    {
        NavigateCommand = new DelegateCommand<string>(OnNavigate);
    }
    
    /// <summary>
    /// Event fired when navigation is requested.
    /// </summary>
    public event EventHandler<App.UI.NavigationEventArgs>? NavigationRequested;

    /// <summary>
    /// Gets or sets whether dark theme is enabled.
    /// </summary>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                ModifyTheme(theme => theme.SetBaseTheme(value ? BaseTheme.Dark : BaseTheme.Light));
            }
        }
    }

    /// <summary>
    /// Command to navigate to different pages.
    /// </summary>
    public ICommand NavigateCommand { get; }

    private void OnNavigate(string? navigationPath)
    {
        if (string.IsNullOrEmpty(navigationPath)) return;

        NavigationRequested?.Invoke(this, new App.UI.NavigationEventArgs(navigationPath));
    }

    private static void ModifyTheme(Action<Theme> modificationAction)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        modificationAction?.Invoke(theme);
        paletteHelper.SetTheme(theme);
    }
}