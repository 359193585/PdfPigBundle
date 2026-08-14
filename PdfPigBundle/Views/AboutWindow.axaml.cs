using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Lang.Avalonia;
using PdfPigBundle.Services;

namespace PdfPigBundle.Views;

public partial class AboutWindow : Window
{

    public AboutWindow()
    {
        InitializeComponent();
        this.DataContext = this;
        this.Icon = App.AppIcon;

        VersionTextBlock.Text = GetExeVersion();
        UpdateStatusTextBlock.Text = "";

    }

    private static string GetExeVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var versionString = version != null
            ? $"Version {version.Major}.{version.Minor}.{version.Build}"
            : "Version 1.0.0";

        return versionString;
    }
    private async void OnCheckForUpdatesClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;
        button.IsEnabled = false;
        UpdateStatusTextBlock.Text = T("Status_CheckingUpdate");
        UpdateStatusTextBlock.Foreground = Avalonia.Media.Brushes.Gray;

        try
        {
            var latest = await UpdateService.GetLatestVersionAsync();
            if (string.IsNullOrEmpty(latest))
            {
                UpdateStatusTextBlock.Text = T("Status_CheckUpdateFailed");
                UpdateStatusTextBlock.Foreground = Avalonia.Media.Brushes.Red;
                return;
            }

            var current = UpdateService.GetCurrentVersion();
            if (UpdateService.CompareVersions(latest, current) > 0)
            {
                UpdateStatusTextBlock.Text = T("Status_NewVersionAvailable", latest);
                UpdateStatusTextBlock.Foreground = Avalonia.Media.Brushes.Blue;
                UpdateStatusTextBlock.Cursor = new Cursor(StandardCursorType.Hand);
                UpdateStatusTextBlock.PointerPressed += (s, e) =>
                {
                    UpdateService.OpenDownloadPage();
                };
            }
            else
            {
                UpdateStatusTextBlock.Text = T("Status_UpToDate");
                UpdateStatusTextBlock.Foreground = Avalonia.Media.Brushes.Green;
                UpdateStatusTextBlock.Cursor = Cursor.Default;
            }
        }
        catch (Exception)
        {
            UpdateStatusTextBlock.Text = T("Status_CheckUpdateFailed");
            UpdateStatusTextBlock.Foreground = Avalonia.Media.Brushes.Red;
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
    private static string T(string key, params object[] args)
    {
        var value = I18nManager.Instance.GetResource(key);
        if (string.IsNullOrEmpty(value))
            return key;
        return args.Length > 0 ? string.Format(value, args) : value;
    }
    private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
