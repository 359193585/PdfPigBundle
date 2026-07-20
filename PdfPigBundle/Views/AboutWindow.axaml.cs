using System.Reflection;
using Avalonia.Controls;

namespace PdfPigBundle.Views;

public partial class AboutWindow : Window
{

    public AboutWindow()
    {
        InitializeComponent();
        this.DataContext = this;
        this.Icon = App.AppIcon;

        VersionTextBlock.Text = GetExeVersion();
    }

    private static string GetExeVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var versionString = version != null
            ? $"版本 {version.Major}.{version.Minor}.{version.Build}"
            : "版本 1.0.0";

        return versionString;
    }

    private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
