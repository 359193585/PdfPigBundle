using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using PdfPigBundle.Views;

namespace PdfPigBundle
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                AppIcon = GetPlatformIcon();

            }

            base.OnFrameworkInitializationCompleted();
        }

        public static WindowIcon? AppIcon { get; private set; }
        private static WindowIcon GetPlatformIcon()
        {
            string iconPath = OperatingSystem.IsWindows()
                ? "avares://PDFMerger/Assets/icon.ico"
                : OperatingSystem.IsMacOS()
                ? "avares://PDFMerger/Assets/icon.png"
                : "avares://PDFMerger/Assets/icon.png";

            var uri = new Uri(iconPath);
            using var stream = AssetLoader.Open(uri);
            return new WindowIcon(stream);
        }
        private  async void OnAboutClick(object? sender, EventArgs e)
        {
            await ShowAboutDialogAsync();
        }

        public static async Task ShowAboutDialogAsync()
        {
            try
            {
                var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                var owner = lifetime?.Windows.FirstOrDefault(w => w.IsVisible);
                if (owner == null) return;
                var aboutWindow = new AboutWindow();
                await aboutWindow.ShowDialog(owner);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"About dialog error: {ex.Message}");
            }
        }
    }
}
