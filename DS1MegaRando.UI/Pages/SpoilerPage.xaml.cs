using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DS1MegaRando.UI.Pages;

public partial class SpoilerPage : UserControl
{
    public string? Log
    {
        get => SpoilerText.Text;
        set => SpoilerText.Text = string.IsNullOrEmpty(value)
            ? "No spoiler log yet. Run the randomizer to generate one."
            : value;
    }

    public SpoilerPage() => InitializeComponent();

    private void OpenEditor_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SpoilerText.Text)) return;
        string tmp = Path.Combine(Path.GetTempPath(), "ds1_rando_spoiler.txt");
        File.WriteAllText(tmp, SpoilerText.Text);
        Process.Start(new ProcessStartInfo(tmp) { UseShellExecute = true });
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter   = "Text Files (*.txt)|*.txt",
            FileName = "spoiler_log.txt",
        };
        if (dlg.ShowDialog() == true)
            File.WriteAllText(dlg.FileName, SpoilerText.Text);
    }
}
