using Windows.Graphics;
using Windows.Storage.Pickers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using ZipBuddy.Core.Interfaces;
using ZipBuddy.Core.Models;
using ZipBuddy.Core.Services;

namespace ZipBuddy.UI.Views;

public sealed partial class CompressOptionsWindow : Window
{
    private readonly string _sourcePath;
    private readonly ILocalizationService? _loc;
    private bool _transitionedToProgress;

    private string T(string key) => _loc?.GetString(key) ?? key;

    private string T(string key, params object[] args) => _loc?.GetString(key, args) ?? key;

    public CompressOptionsWindow(string sourcePath, bool sendEmailAfter = false)
    {
        InitializeComponent();
        _sourcePath = sourcePath;
        _loc = App.Services.GetService<ILocalizationService>();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ConfigureWindowSizeAndPosition();

        ApplyLocalization();

        SourcePathTextBlock.Text = _sourcePath;
        SendEmailCheckBox.IsChecked = sendEmailAfter;

        DestinationPathBox.Text = CompressionOptionsMapper.BuildDefaultDestination(_sourcePath, ArchiveFormat.Zip);
        UpdateFormatCapabilities();

        Closed += (_, _) =>
        {
            if (!_transitionedToProgress)
            {
                Application.Current.Exit();
            }
        };
    }

    private void ApplyLocalization()
    {
        var title = T("CompressDlg_Title");
        Title = title;
        AppTitleBar.Title = title;
        SourceLabelTextBlock.Text = T("CompressDlg_Source");
        DestinationPathBox.Header = T("CompressDlg_Destination");
        DestinationPathBox.PlaceholderText = T("CompressDlg_DestinationPlaceholder");
        BrowseButton.Content = T("Action_Browse");
        FormatComboBox.Header = T("CompressDlg_Format");
        LevelComboBox.Header = T("CompressDlg_Level");
        ArchivePasswordBox.Header = T("CompressDlg_Password");
        ArchivePasswordBox.PlaceholderText = T("CompressDlg_PasswordPlaceholder");
        SendEmailCheckBox.Content = T("CompressDlg_Email");
        CompressButton.Content = T("CompressDlg_Compress");
        CancelButton.Content = T("Action_Cancel");

        SetItems(FormatComboBox.Items,
        [
            T("CompressDlg_FormatZip"),
            T("CompressDlg_Format7z"),
            T("CompressDlg_FormatTar"),
            T("CompressDlg_FormatGZip")
        ]);

        SetItems(LevelComboBox.Items,
        [
            T("CompressDlg_LevelStore"),
            T("CompressDlg_LevelFast"),
            T("CompressDlg_LevelNormal"),
            T("CompressDlg_LevelMaximum"),
            T("CompressDlg_LevelUltra")
        ]);
    }

    private static void SetItems(ItemCollection items, IReadOnlyList<string> labels)
    {
        for (var i = 0; i < labels.Count && i < items.Count; i++)
        {
            if (items[i] is ComboBoxItem item)
                item.Content = labels[i];
        }
    }

    private void ConfigureWindowSizeAndPosition()
    {
        try
        {
            AppWindow.SetIcon("Assets/AppIcon.ico");
        }
        catch { }

        const int width = 540;
        const int height = 450;
        AppWindow.Resize(new SizeInt32(width, height));

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        try
        {
            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                var x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - width) / 2;
                var y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - height) / 2;
                AppWindow.Move(new PointInt32(x, y));
            }
        }
        catch { }
    }

    private void OnFormatChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DestinationPathBox == null || string.IsNullOrWhiteSpace(DestinationPathBox.Text)) return;

        var format = CompressionOptionsMapper.MapFormat(FormatComboBox.SelectedIndex);
        DestinationPathBox.Text = CompressionOptionsMapper.ReplaceExtension(DestinationPathBox.Text, format);
        UpdateFormatCapabilities();
    }

    private void UpdateFormatCapabilities()
    {
        var format = CompressionOptionsMapper.MapFormat(FormatComboBox.SelectedIndex);
        var supportsExtras = CompressionOptionsMapper.SupportsCompressionLevel(format);

        LevelComboBox.IsEnabled = supportsExtras;
        ArchivePasswordBox.IsEnabled = CompressionOptionsMapper.SupportsPassword(format);

        if (!supportsExtras)
        {
            LevelComboBox.SelectedIndex = 2; // Normal
            ArchivePasswordBox.Password = string.Empty;
        }
    }

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(this));

        var format = CompressionOptionsMapper.MapFormat(FormatComboBox.SelectedIndex);
        var ext = CompressionOptionsMapper.GetFilePickerExtension(format);

        savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
        savePicker.FileTypeChoices.Add(T("CompressDlg_FileTypeChoice"), [ext]);
        savePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(DestinationPathBox.Text);

        var file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            DestinationPathBox.Text = file.Path;
        }
    }

    private async void OnCompressClick(object sender, RoutedEventArgs e)
    {
        var dest = DestinationPathBox.Text.Trim();
        var validation = CompressionOptionsMapper.ValidateDestination(dest, _sourcePath);

        if (validation != DestinationValidation.Valid)
        {
            var messageKey = validation switch
            {
                DestinationValidation.Empty => "CompressDlg_ErrorEmpty",
                DestinationValidation.SameAsSource => "CompressDlg_ErrorSameAsSource",
                _ => "CompressDlg_ErrorInvalidPath"
            };
            await ShowMessageAsync("CompressDlg_ErrorTitle", messageKey, T("Action_Close"), null);
            return;
        }

        var format = CompressionOptionsMapper.MapFormat(FormatComboBox.SelectedIndex);
        var level = format.GetCompatibleCompressionLevel(
            CompressionOptionsMapper.MapLevel(LevelComboBox.SelectedIndex));

        var password = string.IsNullOrWhiteSpace(ArchivePasswordBox.Password) ? null : ArchivePasswordBox.Password;
        if (password != null && !CompressionOptionsMapper.SupportsPassword(format))
        {
            await ShowMessageAsync("CompressDlg_ErrorTitle", "CompressDlg_ErrorPassword", T("Action_Close"), null);
            return;
        }

        if (File.Exists(dest))
        {
            var confirmed = await ShowMessageAsync(
                "CompressDlg_OverwriteTitle",
                T("CompressDlg_OverwriteMessage", dest),
                T("Conflict_Overwrite"),
                T("Action_Cancel"));

            if (!confirmed)
                return;
        }

        var options = new CompressionOptions
        {
            Level = level,
            Password = password
        };

        _transitionedToProgress = true;
        Close();

        var progressWin = new TaskProgressWindow(
            TaskProgressWindow.TaskType.Compress,
            primaryPath: dest,
            destinationPath: dest,
            compressFormat: format,
            sourcePaths: [_sourcePath],
            compressionOptions: options,
            openEmailAfter: SendEmailCheckBox.IsChecked ?? false);

        progressWin.Activate();
    }

    private async Task<bool> ShowMessageAsync(string titleKey, string message, string primaryText, string? closeText)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = ((FrameworkElement)Content).XamlRoot,
            Title = T(titleKey),
            Content = message,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = closeText == null ? ContentDialogButton.Primary : ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
