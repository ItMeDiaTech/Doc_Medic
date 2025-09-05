using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
//using Prism.Regions; // TODO: Fix regions namespace issue
using MaterialDesignThemes.Wpf;

namespace App.UI.ViewModels;

/// <summary>
/// Main window view model handling navigation and theme management.
/// </summary>
public class MainWindowViewModel : BindableBase
{
    //private readonly IRegionManager _regionManager; // TODO: Fix regions
    private bool _isDarkTheme;

    public MainWindowViewModel(/* IRegionManager regionManager */)
    {
        //_regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        NavigateCommand = new DelegateCommand<string>(OnNavigate);
        
        // Initialize with Home page
        //_regionManager.RequestNavigate("ContentRegion", "HomePage");
    }

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

        // TODO: Fix regions navigation
        //_regionManager.RequestNavigate("ContentRegion", navigationPath);
    }

    private static void ModifyTheme(Action<Theme> modificationAction)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        modificationAction?.Invoke(theme);
        paletteHelper.SetTheme(theme);
    }
}