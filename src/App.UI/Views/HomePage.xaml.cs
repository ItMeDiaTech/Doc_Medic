using System.Windows;
using System.Windows.Controls;

namespace App.UI.Views;

/// <summary>
/// Interaction logic for HomePage.xaml
/// </summary>
public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void UserControl_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (DataContext is ViewModels.HomePageViewModel viewModel)
            {
                viewModel.HandleDroppedFiles(files);
            }
        }
    }

    private void UserControl_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
}