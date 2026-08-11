using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Lang.Avalonia;
using Lang.Avalonia.Json;
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
            // register the JSON plugin and set the default language to English
            var culture = GetInitialCulture();            // default "en-US"
            I18nManager.Instance.Register(new JsonLangPlugin(), culture, out var error);
            if (!string.IsNullOrEmpty(error))
            {
                // if registration fails, fallback to en-US
                culture = new CultureInfo("en-US");
                I18nManager.Instance.Register(new JsonLangPlugin(), culture, out var _);
                Debug.WriteLine($"I18n fallback to en-US due to error: {error}");
            }


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
        private CultureInfo GetInitialCulture()
        {
            var systemCulture = CultureInfo.CurrentUICulture;
            var i18nFolder = Path.Combine(AppContext.BaseDirectory, "I18n");
            var candidateFile = Path.Combine(i18nFolder, $"{systemCulture.Name}.json");

            // if the exact culture file exists (e.g., zh-CN.json), use it
            if (File.Exists(candidateFile))
                return systemCulture;

            // otherwise, try to match only the language part (e.g., zh-CN falls back to zh if zh.json exists)
            var langOnly = systemCulture.TwoLetterISOLanguageName;
            var langOnlyFile = Path.Combine(i18nFolder, $"{langOnly}.json");
            if (File.Exists(langOnlyFile))
                return new CultureInfo(langOnly);

            // if no matching file is found, fallback to English (en-US)
            return new CultureInfo("en-US");
        }
    }
}
