using System.Globalization;
using ZipBuddy.Core.Interfaces;
using ZipBuddy.Core.Models;
using ZipBuddy.Infrastructure.Logging;
using ZipBuddy.Windows.Notifications;
using ZipBuddy.Windows.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace ZipBuddy.UI.Views;

public sealed partial class TaskProgressWindow : Window, IDisposable
{
    public enum TaskType { Extract, Compress }

    private readonly TaskType _taskType;
    private readonly string _primaryPath;
    private readonly string _destinationPath;
    private readonly ConflictPolicy _conflictPolicy;
    private readonly ArchiveFormat _compressFormat;
    private readonly IReadOnlyList<string>? _sourcePaths;
    private readonly CompressionOptions? _compressionOptions;

    private readonly IArchiveEngine _engine;
    private readonly ISettingsService _settings;
    private readonly ILoggingService _logger;
    private readonly IWindowsNotificationService _notificationService;
    private readonly ILocalizationService? _loc;

    private readonly CancellationTokenSource _cts = new();
    private bool _isCompleted;
    private bool _hasStarted;
    private long _sourceArchiveLength;

    private bool IsPortuguese() => _loc?.CurrentCulture?.StartsWith("pt", StringComparison.OrdinalIgnoreCase) == true;

    public TaskProgressWindow(
        TaskType taskType,
        string primaryPath,
        string destinationPath,
        ConflictPolicy conflictPolicy = ConflictPolicy.Overwrite,
        bool openFolderAfter = false,
        ArchiveFormat compressFormat = ArchiveFormat.Zip,
        IReadOnlyList<string>? sourcePaths = null,
        CompressionOptions? compressionOptions = null)
    {
        InitializeComponent();

        _taskType = taskType;
        _primaryPath = primaryPath;
        _destinationPath = destinationPath;
        _conflictPolicy = conflictPolicy;
        _compressFormat = compressFormat;
        _sourcePaths = sourcePaths;
        _compressionOptions = compressionOptions ?? CompressionOptions.Default;

        _engine = App.Services.GetRequiredService<IArchiveEngine>();
        _settings = App.Services.GetRequiredService<ISettingsService>();
        _logger = App.Services.GetRequiredService<ILoggingService>();
        _notificationService = App.Services.GetRequiredService<IWindowsNotificationService>();
        _loc = App.Services.GetService<ILocalizationService>();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ConfigureWindowSizeAndPosition();

        // Initialize tickers from settings and constructor options
        OpenFolderCheckBox.IsChecked = openFolderAfter || _settings.OpenExtractedFolderAfterExtraction;
        KeepWindowOpenCheckBox.IsChecked = _settings.KeepTaskProgressWindowOpen;

        var isPt = IsPortuguese();
        var fileName = Path.GetFileName(_primaryPath);
        if (_taskType == TaskType.Extract)
        {
            Title = isPt ? $"Extraindo {fileName} — Zip Buddy" : $"Extracting {fileName} — Zip Buddy";
            TitleTextBlock.Text = isPt ? $"Extraindo {fileName}" : $"Extracting {fileName}";
            OperationIcon.Glyph = "\uE8B7"; // Folder/Open
            if (File.Exists(_primaryPath))
            {
                try { _sourceArchiveLength = new FileInfo(_primaryPath).Length; } catch { }
            }
        }
        else
        {
            Title = isPt ? $"Compactando {fileName} — Zip Buddy" : $"Compressing {fileName} — Zip Buddy";
            TitleTextBlock.Text = isPt ? $"Compactando para {fileName}" : $"Compressing to {fileName}";
            OperationIcon.Glyph = "\uE8B9"; // Save/Archive
        }

        if (isPt)
        {
            OpenFolderCheckBox.Content = "Abrir pasta após extrair";
            KeepWindowOpenCheckBox.Content = "Manter janela aberta após concluir";
            DetailsExpander.Header = "Ver mais detalhes...";
            RatioLabelTextBlock.Text = "Taxa de Compressão:";
            SizesLabelTextBlock.Text = "Tamanhos (Original / Compactado):";
            ElapsedLabelTextBlock.Text = "Tempo Decorrido:";
            SavingsLabelTextBlock.Text = "Espaço Economizado:";
            SpeedTextBlock.Text = "Velocidade: Calculando...";
            EtaTextBlock.Text = "Tempo restante: Calculando...";
            ProcessedCountTextBlock.Text = "0 de 0 itens";
            CancelCloseButton.Content = "Cancelar";
            OpenFolderButton.Content = "Abrir Pasta";
            DeleteSourceArchiveButton.Content = "Excluir arquivo de origem (.zip)";
        }
        else
        {
            OpenFolderCheckBox.Content = "Open folder after extracting";
            KeepWindowOpenCheckBox.Content = "Keep window open when completed";
            DetailsExpander.Header = "See more details...";
            RatioLabelTextBlock.Text = "Compression Ratio:";
            SizesLabelTextBlock.Text = "Sizes (Original / Compressed):";
            ElapsedLabelTextBlock.Text = "Elapsed Time:";
            SavingsLabelTextBlock.Text = "Space Saved:";
            SpeedTextBlock.Text = "Speed: Calculating...";
            EtaTextBlock.Text = "Remaining: Calculating...";
            ProcessedCountTextBlock.Text = "0 of 0 items";
            CancelCloseButton.Content = "Cancel";
            OpenFolderButton.Content = "Open Folder";
            DeleteSourceArchiveButton.Content = "Delete source archive";
        }

        Closed += (_, _) =>
        {
            if (!_isCompleted)
                _cts.Cancel();
            _cts.Dispose();
            Application.Current.Exit();
        };

        Activated += OnWindowActivated;
    }

    private const int WindowWidth = 560;
    private const int CollapsedHeight = 315;
    private const int ExpandedHeight = 475;

    private void ConfigureWindowSizeAndPosition()
    {
        try
        {
            AppWindow.SetIcon("Assets/AppIcon.ico");
        }
        catch { }

        AppWindow.Resize(new SizeInt32(WindowWidth, CollapsedHeight));

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
                var x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - WindowWidth) / 2;
                var y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - CollapsedHeight) / 2;
                AppWindow.Move(new PointInt32(x, y));
            }
        }
        catch { }
    }

    private void OnDetailsExpanderExpanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        ResizeWindowHeight(ExpandedHeight);
    }

    private void OnDetailsExpanderCollapsed(Expander sender, ExpanderCollapsedEventArgs args)
    {
        ResizeWindowHeight(CollapsedHeight);
    }

    private void ResizeWindowHeight(int targetHeight)
    {
        try
        {
            if (AppWindow == null) return;

            var currentPos = AppWindow.Position;
            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            int newY = currentPos.Y;

            if (displayArea != null)
            {
                var workAreaBottom = displayArea.WorkArea.Y + displayArea.WorkArea.Height;
                if (newY + targetHeight > workAreaBottom)
                {
                    newY = Math.Max(displayArea.WorkArea.Y, workAreaBottom - targetHeight);
                }
            }

            if (newY != currentPos.Y)
            {
                AppWindow.MoveAndResize(new RectInt32(currentPos.X, newY, WindowWidth, targetHeight));
            }
            else
            {
                AppWindow.Resize(new SizeInt32(WindowWidth, targetHeight));
            }
        }
        catch { }
    }

    private async void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_hasStarted) return;
        _hasStarted = true;

        await Task.Delay(100); // Allow XAML tree to settle
        if (_taskType == TaskType.Extract)
        {
            await RunExtractionAsync();
        }
        else
        {
            await RunCompressionAsync();
        }
    }

    private async Task RunExtractionAsync()
    {
        var archivePath = _primaryPath;
        var destinationDir = _destinationPath;

        try
        {
            string? password = null;
            ArchiveInfo? archiveInfo = null;
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    archiveInfo = await _engine.OpenArchiveAsync(archivePath, password, _cts.Token);
                    if (string.IsNullOrEmpty(password) && (archiveInfo.IsEncrypted || archiveInfo.Entries.Any(e => e.IsEncrypted)))
                    {
                        password = await PromptPasswordAsync();
                        if (string.IsNullOrEmpty(password))
                        {
                            FinishWithCancellation();
                            return;
                        }
                    }
                    break;
                }
                catch (Exception ex) when (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
                {
                    password = await PromptPasswordAsync();
                    if (string.IsNullOrEmpty(password))
                    {
                        FinishWithCancellation();
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    FinishWithCancellation();
                    return;
                }
                catch (Exception ex)
                {
                    FinishWithError($"Não foi possível abrir o arquivo: {ex.Message}");
                    return;
                }
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var isPt = IsPortuguese();
            var speedPrefix = isPt ? "Velocidade" : "Speed";
            var etaPrefix = isPt ? "Tempo restante" : "Remaining";
            var itemsWord = isPt ? "itens" : "items";
            var ofWord = isPt ? "de" : "of";
            var calculatingWord = isPt ? "Calculando..." : "Calculating...";
            var actionWord = isPt ? "Extraindo..." : "Extracting...";

            var progress = new Progress<OperationProgress>(p =>
            {
                // Item Progress Bar
                ItemProgressBar.Value = p.ItemPercentage;
                ItemPercentageTextBlock.Text = $"{p.ItemPercentage:0}%";
                CurrentItemTextBlock.Text = string.IsNullOrWhiteSpace(p.CurrentItemName) ? actionWord : p.CurrentItemName;

                // Total Progress Bar
                TaskProgressBar.Value = p.Percentage;
                TotalPercentageTextBlock.Text = $"{p.Percentage:0}%";
                ProcessedCountTextBlock.Text = $"{p.FilesProcessed} {ofWord} {p.TotalFiles} {itemsWord} ({ArchiveEntry.FormatBytes(p.BytesProcessed)} {ofWord} {ArchiveEntry.FormatBytes(p.TotalBytes)})";

                // Speed and ETA
                var speedVal = p.BytesPerSecond > 0
                    ? $"{ArchiveEntry.FormatBytes((long)p.BytesPerSecond)}/s"
                    : calculatingWord;
                var etaVal = p.EstimatedTimeRemaining.HasValue
                    ? (p.EstimatedTimeRemaining.Value.TotalHours >= 1
                        ? $"{p.EstimatedTimeRemaining.Value:hh\\:mm\\:ss}"
                        : $"{p.EstimatedTimeRemaining.Value:mm\\:ss}")
                    : calculatingWord;

                SpeedTextBlock.Text = $"{speedPrefix}: {speedVal}";
                EtaTextBlock.Text = $"{etaPrefix}: {etaVal}";

                // Compression Details Expander
                ElapsedTextBlock.Text = stopwatch.Elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
                if (p.TotalBytes > 0 && _sourceArchiveLength > 0)
                {
                    var ratio = Math.Clamp((double)_sourceArchiveLength / p.TotalBytes * 100.0, 0.0, 100.0);
                    var savings = Math.Max(0, 100.0 - ratio);
                    RatioTextBlock.Text = $"{ratio:F1}%";
                    SavingsTextBlock.Text = $"{savings:F1}% de espaço economizado";
                    SizesTextBlock.Text = $"Original: {ArchiveEntry.FormatBytes(p.TotalBytes)} | Compactado: {ArchiveEntry.FormatBytes(_sourceArchiveLength)}";
                }
                else
                {
                    SizesTextBlock.Text = $"Processado: {ArchiveEntry.FormatBytes(p.BytesProcessed)}";
                }
            });

            var request = new ExtractionRequest
            {
                ArchiveFilePath = archivePath,
                DestinationDirectory = destinationDir,
                DefaultConflictPolicy = _conflictPolicy,
                ConflictCallback = PromptConflictAsync,
                Password = password,
                Progress = progress
            };

            var result = await _engine.ExtractAsync(request, _cts.Token);

            while (!result.Success && !result.IsCancelled && !_cts.IsCancellationRequested &&
                   (result.ErrorMessage?.Contains("password", StringComparison.OrdinalIgnoreCase) == true ||
                    result.ErrorMessage?.Contains("cryptographic", StringComparison.OrdinalIgnoreCase) == true ||
                    result.ErrorMessage?.Contains("encryption", StringComparison.OrdinalIgnoreCase) == true))
            {
                password = await PromptPasswordAsync();
                if (string.IsNullOrEmpty(password))
                {
                    FinishWithCancellation();
                    return;
                }

                request = request with { Password = password };
                result = await _engine.ExtractAsync(request, _cts.Token);
            }

            if (result.Success)
            {
                await FinishWithSuccess(
                    title: isPt ? "Extração concluída" : "Extraction complete",
                    message: isPt
                        ? $"Extraídos {result.FilesProcessed} arquivo(s) para {destinationDir}"
                        : $"Extracted {result.FilesProcessed} file(s) to {destinationDir}",
                    openFolderTarget: destinationDir);
            }
            else if (result.IsCancelled)
            {
                FinishWithCancellation();
            }
            else
            {
                FinishWithError(result.ErrorMessage ?? "A extração falhou.");
            }
        }
        catch (OperationCanceledException)
        {
            FinishWithCancellation();
        }
        catch (Exception ex)
        {
            FinishWithError($"Erro inesperado: {ex.Message}");
        }
    }

    private async Task RunCompressionAsync()
    {
        var outputArchive = _primaryPath;
        var sourcePaths = _sourcePaths ?? [];

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var isPt = IsPortuguese();
            var speedPrefix = isPt ? "Velocidade" : "Speed";
            var etaPrefix = isPt ? "Tempo restante" : "Remaining";
            var itemsWord = isPt ? "itens" : "items";
            var ofWord = isPt ? "de" : "of";
            var calculatingWord = isPt ? "Calculando..." : "Calculating...";
            var actionWord = isPt ? "Compactando..." : "Compressing...";

            var progress = new Progress<OperationProgress>(p =>
            {
                ItemProgressBar.Value = p.ItemPercentage;
                ItemPercentageTextBlock.Text = $"{p.ItemPercentage:0}%";
                CurrentItemTextBlock.Text = string.IsNullOrWhiteSpace(p.CurrentItemName) ? actionWord : p.CurrentItemName;

                TaskProgressBar.Value = p.Percentage;
                TotalPercentageTextBlock.Text = $"{p.Percentage:0}%";
                ProcessedCountTextBlock.Text = $"{p.FilesProcessed} {ofWord} {p.TotalFiles} {itemsWord} ({ArchiveEntry.FormatBytes(p.BytesProcessed)} {ofWord} {ArchiveEntry.FormatBytes(p.TotalBytes)})";

                var speedVal = p.BytesPerSecond > 0
                    ? $"{ArchiveEntry.FormatBytes((long)p.BytesPerSecond)}/s"
                    : calculatingWord;
                var etaVal = p.EstimatedTimeRemaining.HasValue
                    ? (p.EstimatedTimeRemaining.Value.TotalHours >= 1
                        ? $"{p.EstimatedTimeRemaining.Value:hh\\:mm\\:ss}"
                        : $"{p.EstimatedTimeRemaining.Value:mm\\:ss}")
                    : calculatingWord;

                SpeedTextBlock.Text = $"{speedPrefix}: {speedVal}";
                EtaTextBlock.Text = $"{etaPrefix}: {etaVal}";

                ElapsedTextBlock.Text = stopwatch.Elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
                SizesTextBlock.Text = $"{(isPt ? "Processados" : "Processed")}: {ArchiveEntry.FormatBytes(p.BytesProcessed)}";
            });

            var request = new CompressionRequest
            {
                OutputArchiveFilePath = outputArchive,
                Format = _compressFormat,
                SourcePaths = sourcePaths,
                Options = _compressionOptions ?? CompressionOptions.Default,
                Progress = progress
            };

            var result = await _engine.CreateArchiveAsync(request, _cts.Token);
            if (result.Success)
            {
                await FinishWithSuccess(
                    title: isPt ? "Compactação concluída" : "Compression complete",
                    message: isPt
                        ? $"Arquivo {Path.GetFileName(outputArchive)} criado com sucesso."
                        : $"Archive {Path.GetFileName(outputArchive)} created successfully.",
                    openFolderTarget: Path.GetDirectoryName(outputArchive));
            }
            else if (result.IsCancelled)
            {
                FinishWithCancellation();
            }
            else
            {
                FinishWithError(result.ErrorMessage ?? "A compactação falhou.");
            }
        }
        catch (OperationCanceledException)
        {
            FinishWithCancellation();
        }
        catch (Exception ex)
        {
            FinishWithError($"Erro inesperado: {ex.Message}");
        }
    }

    private async Task FinishWithSuccess(string title, string message, string? openFolderTarget)
    {
        _isCompleted = true;
        ItemProgressBar.Value = 100;
        TaskProgressBar.Value = 100;
        ItemPercentageTextBlock.Text = "100%";
        TotalPercentageTextBlock.Text = "100%";
        CurrentItemTextBlock.Text = "Concluído com sucesso";

        if (_settings.NotifyOnTaskCompletion)
        {
            _notificationService.TryShow(title, message);
        }

        // Automatic archive deletion if configured globally
        if (_taskType == TaskType.Extract && _settings.DeleteArchiveAfterExtraction && File.Exists(_primaryPath))
        {
            try
            {
                File.Delete(_primaryPath);
                _logger.Info($"Deleted source archive after successful extraction: {_primaryPath}");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not auto-delete source archive: {ex.Message}");
            }
        }

        // Open folder if ticker checked
        var shouldOpenFolder = OpenFolderCheckBox.IsChecked == true;
        if (shouldOpenFolder && !string.IsNullOrEmpty(openFolderTarget))
        {
            ShellOperations.OpenFolderAndSelect(openFolderTarget);
        }

        var shouldKeepOpen = KeepWindowOpenCheckBox.IsChecked == true;
        if (shouldKeepOpen)
        {
            CancelCloseButton.Content = "Fechar";
            if (!string.IsNullOrEmpty(openFolderTarget))
            {
                OpenFolderButton.Visibility = Visibility.Visible;
            }

            // Exibir opção de excluir arquivo de origem se ainda existir
            if (_taskType == TaskType.Extract && File.Exists(_primaryPath))
            {
                DeleteSourceArchiveButton.Visibility = Visibility.Visible;
            }
        }
        else
        {
            await Task.Delay(400);
            Close();
        }
    }

    private void FinishWithError(string errorMessage)
    {
        _isCompleted = true;
        _logger.Error(errorMessage);
        TaskProgressBar.Visibility = Visibility.Collapsed;
        ItemProgressBar.Visibility = Visibility.Collapsed;
        CurrentItemTextBlock.Visibility = Visibility.Collapsed;
        SpeedTextBlock.Visibility = Visibility.Collapsed;
        EtaTextBlock.Visibility = Visibility.Collapsed;
        ProcessedCountTextBlock.Visibility = Visibility.Collapsed;

        StatusMessageTextBlock.Text = errorMessage;
        StatusMessageTextBlock.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        StatusMessageTextBlock.Visibility = Visibility.Visible;

        CancelCloseButton.Content = "Fechar";
    }

    private void FinishWithCancellation()
    {
        _isCompleted = true;
        TaskProgressBar.Visibility = Visibility.Collapsed;
        ItemProgressBar.Visibility = Visibility.Collapsed;
        CurrentItemTextBlock.Text = "Operação cancelada";
        SpeedTextBlock.Visibility = Visibility.Collapsed;
        EtaTextBlock.Visibility = Visibility.Collapsed;
        ProcessedCountTextBlock.Visibility = Visibility.Collapsed;

        StatusMessageTextBlock.Text = "A operação foi cancelada pelo usuário.";
        StatusMessageTextBlock.Visibility = Visibility.Visible;

        CancelCloseButton.Content = "Fechar";
    }

    private void OnDeleteSourceArchiveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(_primaryPath))
            {
                File.Delete(_primaryPath);
                DeleteSourceArchiveButton.IsEnabled = false;
                StatusMessageTextBlock.Text = "Arquivo de origem (.zip) excluído com sucesso.";
                StatusMessageTextBlock.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];
                StatusMessageTextBlock.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            StatusMessageTextBlock.Text = $"Não foi possível excluir o arquivo: {ex.Message}";
            StatusMessageTextBlock.Visibility = Visibility.Visible;
        }
    }

    private async Task<string?> PromptPasswordAsync()
    {
        var dialog = new PasswordDialog { XamlRoot = Content.XamlRoot };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? dialog.Password : null;
    }

    private async Task<ConflictResolution> PromptConflictAsync(string existingFilePath)
    {
        var dialog = new ConflictDialog(existingFilePath) { XamlRoot = Content.XamlRoot };
        await dialog.ShowAsync();
        return dialog.Resolution;
    }

    private void OnCancelCloseClick(object sender, RoutedEventArgs e)
    {
        if (_isCompleted)
        {
            Close();
        }
        else
        {
            _cts.Cancel();
            CancelCloseButton.IsEnabled = false;
            CurrentItemTextBlock.Text = "Cancelando...";
        }
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        var target = _taskType == TaskType.Extract ? _destinationPath : Path.GetDirectoryName(_primaryPath);
        if (!string.IsNullOrEmpty(target))
        {
            ShellOperations.OpenFolderAndSelect(target);
        }
        Close();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
