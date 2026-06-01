using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DS1MegaRando.UI.Controls;

public partial class ProgressOverlay : UserControl
{
    public ProgressOverlay()
    {
        InitializeComponent();
    }

    public void Reset()
    {
        LogText.Text   = "";
        ProgressBar.Value = 0;
        TitleText.Text = "RANDOMIZING...";
        TitleText.Foreground = (Brush)FindResource("DS1Gold");
        CloseBtn.Visibility  = Visibility.Collapsed;
    }

    public void AppendLog(string line)
    {
        LogText.Text += line + "\n";
        LogText.ScrollToEnd();
    }

    public void SetProgress(int pct)
    {
        ProgressBar.Value = Math.Clamp(pct, 0, 100);
    }

    public void SetComplete(bool success, string message)
    {
        TitleText.Text = success ? "FIRE LINKED" : "FAILED";
        TitleText.Foreground = success
            ? (Brush)FindResource("DS1Success")
            : (Brush)FindResource("DS1Error");
        AppendLog(success ? $"\n✓ {message}" : $"\n✗ {message}");
        CloseBtn.Visibility = Visibility.Visible;
        ProgressBar.Value   = success ? 100 : ProgressBar.Value;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
    }
}
