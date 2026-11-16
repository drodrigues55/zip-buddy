using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ZipBuddy.Core.Interfaces;
using ZipBuddy.Core.Models;
using ZipBuddy.Core.Services;

namespace ZipBuddy.UI.Views;

public sealed partial class ChecksumWindow : Window, IDisposable
{
    private readonly string _filePath;
    private readonly ILocalizationService? _loc;
    private readonly CancellationTokenSource _cts = new();
    private ChecksumResult? _result;
    private bool _disposed;

    private string T(string key) => _loc?.GetString(key) ?? key;
    private string T(string key, params object[] args) => _loc?.GetString(key, args) ?? key;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    public ChecksumWindow(string filePath)
    {
        InitializeComponent();
        _filePath = filePath;
        _loc = App.Services.GetService<ILocalizationService>();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ConfigureWindowSizeAndPosition();

        var fileName = Path.GetFileName(_filePath);
        FileNameTextBlock.Text = fileName;

        var title = T("Checksum_Title");
        Title = $"{title} — {fileName}";
        AppTitleBar.Title = Title;

        FileSizeTextBlock.Text = T("Checksum_SizeCalculating");
        var calculating = T("Checksum_Calculating");
        Crc32Box.Text = calculating;
        Crc64Box.Text = calculating;
        Sha1Box.Text = calculating;
        Sha256Box.Text = calculating;

        CopyCrc32Button.Content = T("Checksum_Copy");
        CopyCrc64Button.Content = T("Checksum_Copy");
        CopySha1Button.Content = T("Checksum_Copy");
        CopySha256Button.Content = T("Checksum_Copy");
        CopyAllButton.Content = T("Checksum_CopyAll");
        CloseButton.Content = T("Action_Close");
        StatusTextBlock.Text = T("Checksum_StatusCalculating");

        Closed += (_, _) =>
        {
            if (!_disposed)
            {
                try { _cts.Cancel(); } catch (ObjectDisposedException) { }
                Dispose();
            }
            Application.Current.Exit();
        };

        Activated += OnWindowActivated;
    }

    private void ConfigureWindowSizeAndPosition()
    {
        try
        {
            AppWindow.SetIcon("Assets/AppIcon.ico");
        }
        catch { }

        const int width = 560;
        const int height = 400;
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

    private async void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnWindowActivated;

        if (!File.Exists(_filePath))
        {
            StatusTextBlock.Text = T("Checksum_FileNotFound");
            CalculationProgressBar.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var fileInfo = new FileInfo(_filePath);
            FileSizeTextBlock.Text = T("Checksum_FileSize", ArchiveEntry.FormatBytes(fileInfo.Length), fileInfo.Length);

            var progress = new Progress<double>(p =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    CalculationProgressBar.IsIndeterminate = false;
                    CalculationProgressBar.Value = p * 100.0;
                });
            });

            _result = await ChecksumCalculator.ComputeAsync(_filePath, progress, _cts.Token);

            CalculationProgressBar.Visibility = Visibility.Collapsed;
            Crc32Box.Text = _result.Crc32;
            Crc64Box.Text = _result.Crc64;
            Sha1Box.Text = _result.Sha1;
            Sha256Box.Text = _result.Sha256;
            CopyAllButton.IsEnabled = true;
            StatusTextBlock.Text = T("Checksum_StatusDone");
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = T("Checksum_StatusCanceled");
        }
        catch (Exception ex)
        {
            CalculationProgressBar.Visibility = Visibility.Collapsed;
            StatusTextBlock.Text = T("Checksum_Error", ex.Message);
        }
    }

    private void CopyToClipboard(string text, string label)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
            StatusTextBlock.Text = T("Checksum_Copied", label);
        }
        catch
        {
            StatusTextBlock.Text = T("Checksum_CopyFailed");
        }
    }

    private void OnCopyCrc32Click(object sender, RoutedEventArgs e) => CopyToClipboard(Crc32Box.Text, "CRC-32");
    private void OnCopyCrc64Click(object sender, RoutedEventArgs e) => CopyToClipboard(Crc64Box.Text, "CRC-64");
    private void OnCopySha1Click(object sender, RoutedEventArgs e) => CopyToClipboard(Sha1Box.Text, "SHA-1");
    private void OnCopySha256Click(object sender, RoutedEventArgs e) => CopyToClipboard(Sha256Box.Text, "SHA-256");

    private void OnCopyAllClick(object sender, RoutedEventArgs e)
    {
        if (_result == null) return;
        var summary = $"""
{T("Checksum_SummaryFile")}: {_result.FileName}
{T("Checksum_SummarySize")}: {_result.FileSize:N0} bytes
CRC-32:  {_result.Crc32}
CRC-64:  {_result.Crc64}
SHA-1:   {_result.Sha1}
SHA-256: {_result.Sha256}
""";
        CopyToClipboard(summary, T("Checksum_CopyAll"));
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
