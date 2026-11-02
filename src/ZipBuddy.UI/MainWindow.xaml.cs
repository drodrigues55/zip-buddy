using ZipBuddy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;

namespace ZipBuddy.UI;

public sealed partial class MainWindow : Window
{
    public static new MainWindow? Current { get; private set; }
    public MainPage? ArchivePage => RootFrame.Content as MainPage;

    public MainWindow()
    {
        Current = this;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var settings = App.Services?.GetService<ISettingsService>();
        if (settings != null && Content is FrameworkElement root)
        {
            root.RequestedTheme = settings.ThemeMode switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }

        try
        {
            AppWindow.SetIcon("Assets/AppIcon.ico");
        }
        catch { }

    }

    public void ShowMainPage()
    {
        RootFrame.Navigate(typeof(MainPage));
    }

    private void OnMainDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop to open in Zip Buddy";
        e.DragUIOverride.IsContentVisible = true;
    }

    private async void OnMainDrop(object sender, DragEventArgs e)
    {
        if (ArchivePage is { } page)
        {
            await page.HandleDropAsync(e);
        }
    }
}
