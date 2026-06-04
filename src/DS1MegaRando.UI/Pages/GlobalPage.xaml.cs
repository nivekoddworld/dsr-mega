using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DS1MegaRando.UI.Pages;

public partial class GlobalPage : UserControl
{
    public GlobalPage() => InitializeComponent();

    private void BrowseGameDir_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select Dark Souls Remastered folder" };
        if (dlg.ShowDialog() == true && DataContext is ViewModels.MainViewModel vm)
            vm.Global.GameDirectory = dlg.FolderName;
    }

    private void BrowseOutputDir_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select output folder" };
        if (dlg.ShowDialog() == true && DataContext is ViewModels.MainViewModel vm)
            vm.Global.OutputDirectory = dlg.FolderName;
    }
}
