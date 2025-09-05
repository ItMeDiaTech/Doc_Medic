using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;

namespace App.UI.ViewModels;

/// <summary>
/// Base class for all view models providing common functionality.
/// </summary>
public abstract class ViewModelBase : BindableBase, INavigationAware
{
    private bool _isBusy;
    private string _title = string.Empty;

    protected ViewModelBase()
    {
        LoadedCommand = new DelegateCommand(OnLoaded);
    }

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Gets or sets whether the view model is in a busy state.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Command executed when the view is loaded.
    /// </summary>
    public ICommand LoadedCommand { get; }

    /// <summary>
    /// Called when the view is loaded.
    /// </summary>
    protected virtual void OnLoaded()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Executes an async operation with busy state management.
    /// </summary>
    protected async Task ExecuteWithBusyState(Func<Task> operation)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            await operation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Executes an async operation with busy state management and return value.
    /// </summary>
    protected async Task<T> ExecuteWithBusyState<T>(Func<Task<T>> operation)
    {
        if (IsBusy) return default(T)!;

        try
        {
            IsBusy = true;
            return await operation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    #region INavigationAware

    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {
        // Override in derived classes
    }

    public virtual bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // Override in derived classes
    }

    #endregion
}