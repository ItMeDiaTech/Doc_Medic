using System.Windows.Controls;
using System.Collections.Specialized;

namespace App.UI.Views;

/// <summary>
/// Interaction logic for LogsPage.xaml
/// </summary>
public partial class LogsPage : UserControl
{
    public LogsPage()
    {
        InitializeComponent();
        
        // Auto-scroll to bottom when new log entries are added
        if (DataContext is ViewModels.LogsPageViewModel viewModel)
        {
            viewModel.LogEntries.CollectionChanged += OnLogEntriesChanged;
        }

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        // Unsubscribe from old view model
        if (e.OldValue is ViewModels.LogsPageViewModel oldViewModel)
        {
            oldViewModel.LogEntries.CollectionChanged -= OnLogEntriesChanged;
        }

        // Subscribe to new view model
        if (e.NewValue is ViewModels.LogsPageViewModel newViewModel)
        {
            newViewModel.LogEntries.CollectionChanged += OnLogEntriesChanged;
        }
    }

    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is ViewModels.LogsPageViewModel viewModel && viewModel.AutoScroll)
        {
            // Scroll to bottom when new entries are added
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    LogScrollViewer.ScrollToEnd();
                });
            }
        }
    }
}