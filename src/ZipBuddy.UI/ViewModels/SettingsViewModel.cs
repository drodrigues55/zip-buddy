using CommunityToolkit.Mvvm.ComponentModel;
using ZipBuddy.Core.Interfaces;
using ZipBuddy.Core.Models;
using ZipBuddy.Windows.Shell;

namespace ZipBuddy.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;
    private readonly ITaskSchedulerService _taskSchedulerService;
    private readonly IUpdateCoordinator _updateCoordinator;
    private readonly ILocalizationService? _localizationService;

    [ObservableProperty]
    private string _language = "System";

    [ObservableProperty]
    private string _defaultExtractionFolder;

    [ObservableProperty]
    private int _maxExtractionThreads = Environment.ProcessorCount;

    [ObservableProperty]
    private string _customTempDirectory = string.Empty;

    [ObservableProperty]
    private bool _openFolderAfterExtraction;

    [ObservableProperty]
    private bool _keepTaskProgressWindowOpen;

    [ObservableProperty]
    private bool _deleteArchiveAfterExtraction;

    [ObservableProperty]
    private string _doubleClickAction = "OpenInZipBuddy";

    [ObservableProperty]
    private bool _confirmBeforeOverwriting;

    [ObservableProperty]
    private bool _confirmBeforeDeleting;

    [ObservableProperty]
    private ArchiveFormat _defaultArchiveFormat;

    [ObservableProperty]
    private CompressionLevel _defaultCompressionLevel;

    [ObservableProperty]
    private string _themeMode;

    [ObservableProperty]
    private bool _enableContextMenu;

    [ObservableProperty]
    private bool _useCascadingContextMenu;

    [ObservableProperty]
    private bool _autoCloseTaskProgressWindow;

    [ObservableProperty]
    private bool _notifyOnTaskCompletion;

    [ObservableProperty]
    private bool _enableOpenWith;

    [ObservableProperty]
    private bool _enableExtractHere;

    [ObservableProperty]
    private bool _enableExtractToFolder;

    [ObservableProperty]
    private bool _enableExtractTo;

    [ObservableProperty]
    private bool _enableCompressToZip;

    [ObservableProperty]
    private bool _enableCompressTo7z;

    [ObservableProperty]
    private bool _autoCheckUpdates;

    [ObservableProperty]
    private string _updateCheckFrequency;

    [ObservableProperty]
    private string _currentVersion = "1.0.0";

    [ObservableProperty]
    private string _lastCheckDisplay = "Nunca";

    [ObservableProperty]
    private string _updateStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _latestVersionAvailable = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(
        ISettingsService settingsService,
        ILoggingService loggingService,
        ITaskSchedulerService taskSchedulerService,
        IUpdateCoordinator updateCoordinator,
        ILocalizationService? localizationService = null)
    {
        _settingsService = settingsService;
        _loggingService = loggingService;
        _taskSchedulerService = taskSchedulerService;
        _updateCoordinator = updateCoordinator;
        _localizationService = localizationService;

        var asmVer = typeof(SettingsViewModel).Assembly.GetName().Version;
        if (asmVer != null)
        {
            _currentVersion = $"{asmVer.Major}.{asmVer.Minor}.{asmVer.Build}";
        }

        _language = _settingsService.Language;
        _defaultExtractionFolder = _settingsService.DefaultExtractionFolder;
        _openFolderAfterExtraction = _settingsService.OpenExtractedFolderAfterExtraction;
        _keepTaskProgressWindowOpen = _settingsService.KeepTaskProgressWindowOpen;
        _deleteArchiveAfterExtraction = _settingsService.DeleteArchiveAfterExtraction;
        _doubleClickAction = _settingsService.DoubleClickAction;
        _confirmBeforeOverwriting = _settingsService.ConfirmBeforeOverwriting;
        _confirmBeforeDeleting = _settingsService.ConfirmBeforeDeleting;
        _defaultArchiveFormat = _settingsService.DefaultArchiveFormat;
        _defaultCompressionLevel = _settingsService.DefaultCompressionLevel;
        _themeMode = _settingsService.ThemeMode;
        _enableContextMenu = _settingsService.EnableContextMenu;
        _useCascadingContextMenu = _settingsService.UseCascadingContextMenu;
        _autoCloseTaskProgressWindow = !_settingsService.KeepTaskProgressWindowOpen;
        _notifyOnTaskCompletion = _settingsService.NotifyOnTaskCompletion;
        _autoCheckUpdates = _settingsService.AutoCheckUpdates;
        _updateCheckFrequency = _settingsService.UpdateCheckFrequency;
        _maxExtractionThreads = _settingsService.MaxExtractionThreads > 0 ? _settingsService.MaxExtractionThreads : Environment.ProcessorCount;
        _customTempDirectory = _settingsService.CustomTempDirectory ?? string.Empty;
        _lastCheckDisplay = _settingsService.LastUpdateCheckTime?.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.CurrentCulture) ?? "Nunca";

        var enabledCommands = _settingsService.EnabledContextMenuCommands;
        _enableOpenWith = enabledCommands.Contains("OpenWith", StringComparer.OrdinalIgnoreCase);
        _enableExtractHere = enabledCommands.Contains("ExtractHere", StringComparer.OrdinalIgnoreCase);
        _enableExtractToFolder = enabledCommands.Contains("ExtractToFolder", StringComparer.OrdinalIgnoreCase);
        _enableExtractTo = enabledCommands.Contains("ExtractTo", StringComparer.OrdinalIgnoreCase);
        _enableCompressToZip = enabledCommands.Contains("CompressToZip", StringComparer.OrdinalIgnoreCase);
        _enableCompressTo7z = enabledCommands.Contains("CompressTo7z", StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> SaveSettingsAsync()
    {
        var prevLanguage = _settingsService.Language;
        var prevTheme = _settingsService.ThemeMode;
        var prevCulture = _localizationService?.CurrentCulture;

        _settingsService.Language = Language;
        _settingsService.DefaultExtractionFolder = DefaultExtractionFolder;
        _settingsService.OpenExtractedFolderAfterExtraction = OpenFolderAfterExtraction;
        _settingsService.KeepTaskProgressWindowOpen = KeepTaskProgressWindowOpen;
        _settingsService.DeleteArchiveAfterExtraction = DeleteArchiveAfterExtraction;
        _settingsService.DoubleClickAction = DoubleClickAction;
        _settingsService.ConfirmBeforeOverwriting = ConfirmBeforeOverwriting;
        if (!DefaultArchiveFormat.SupportsCompressionLevel(DefaultCompressionLevel))
        {
            DefaultCompressionLevel = DefaultArchiveFormat.GetCompatibleCompressionLevel(DefaultCompressionLevel);
        }

        _settingsService.DefaultArchiveFormat = DefaultArchiveFormat;
        _settingsService.DefaultCompressionLevel = DefaultCompressionLevel;
        _settingsService.ThemeMode = ThemeMode;
        _settingsService.EnableContextMenu = EnableContextMenu;
        _settingsService.UseCascadingContextMenu = UseCascadingContextMenu;
        _settingsService.NotifyOnTaskCompletion = NotifyOnTaskCompletion;
        _settingsService.AutoCheckUpdates = AutoCheckUpdates;
        _settingsService.UpdateCheckFrequency = UpdateCheckFrequency;
        _settingsService.MaxExtractionThreads = Math.Clamp(MaxExtractionThreads, 1, 64);
        _settingsService.CustomTempDirectory = CustomTempDirectory ?? string.Empty;

        var enabledCommands = new List<string>();
        if (EnableOpenWith) enabledCommands.Add("OpenWith");
        if (EnableExtractHere) enabledCommands.Add("ExtractHere");
        if (EnableExtractToFolder) enabledCommands.Add("ExtractToFolder");
        if (EnableExtractTo) enabledCommands.Add("ExtractTo");
        if (EnableCompressToZip) enabledCommands.Add("CompressToZip");
        if (EnableCompressTo7z) enabledCommands.Add("CompressTo7z");
        _settingsService.EnabledContextMenuCommands = enabledCommands;

        bool saved;
        try
        {
            saved = await _settingsService.SaveAsync();
        }
        catch
        {
            saved = false;
        }

        if (!saved)
        {
            _settingsService.Language = prevLanguage;
            _settingsService.ThemeMode = prevTheme;
            if (_localizationService != null && prevCulture != null)
            {
                _localizationService.CurrentCulture = prevCulture;
            }

            StatusMessage = _localizationService?["Settings_Save_Failed"]
                ?? "Failed to save settings. Please verify permissions and available disk space.";
            return false;
        }

        if (_localizationService != null && !string.IsNullOrWhiteSpace(Language))
        {
            _localizationService.CurrentCulture = Language;
        }

        bool integrationWarning = false;

        // Sync with Windows Task Scheduler
        try
        {
            var updaterPath = _updateCoordinator.LocateUpdaterExecutable();
            if (AutoCheckUpdates && !string.IsNullOrEmpty(updaterPath))
            {
                await _taskSchedulerService.RegisterOrUpdateTaskAsync(updaterPath, UpdateCheckFrequency);
            }
            else
            {
                await _taskSchedulerService.UnregisterTaskAsync();
            }
        }
        catch (Exception ex)
        {
            _loggingService.Warn($"Failed to update scheduled updater task: {ex.Message}");
            integrationWarning = true;
        }

        try
        {
            var exePath = Environment.ProcessPath ?? string.Empty;
            bool contextMenuOk;
            if (EnableContextMenu && !string.IsNullOrEmpty(exePath))
            {
                contextMenuOk = ContextMenuManager.RegisterContextMenu(
                    exePath,
                    enableOpenWith: EnableOpenWith,
                    enableExtractHere: EnableExtractHere,
                    enableExtractToFolder: EnableExtractToFolder,
                    enableExtractTo: EnableExtractTo,
                    enableCompressToZip: EnableCompressToZip,
                    enableCompressTo7z: EnableCompressTo7z,
                    useCascadingMenu: UseCascadingContextMenu);
            }
            else
            {
                contextMenuOk = ContextMenuManager.UnregisterContextMenu();
            }

            if (!contextMenuOk)
            {
                integrationWarning = true;
            }
        }
        catch (Exception ex)
        {
            _loggingService.Warn($"Failed to update Windows Explorer context menu: {ex.Message}");
            integrationWarning = true;
        }

        StatusMessage = integrationWarning
            ? (_localizationService?["Settings_Save_IntegrationWarning"] ?? "Settings saved, but Windows integration could not be updated.")
            : (_localizationService?["Settings_Save_Success"] ?? "Settings saved successfully.");

        return true;
    }

    public async Task CheckForUpdatesNowAsync()
    {
        IsCheckingForUpdates = true;
        UpdateStatusMessage = "Verificando atualizações...";
        try
        {
            var result = await _updateCoordinator.CheckForUpdatesAsync();
            if (result.IsUpdateAvailable)
            {
                IsUpdateAvailable = true;
                LatestVersionAvailable = result.LatestVersion;
                UpdateStatusMessage = $"Nova versão disponível: v{result.LatestVersion}";
            }
            else if (string.Equals(result.Status, "UpToDate", StringComparison.OrdinalIgnoreCase))
            {
                IsUpdateAvailable = false;
                UpdateStatusMessage = "Você está usando a versão mais recente.";
            }
            else if (string.Equals(result.Status, "SignatureInvalid", StringComparison.OrdinalIgnoreCase))
            {
                IsUpdateAvailable = false;
                UpdateStatusMessage = "Atualização recusada: assinatura digital inválida ou pacote adulterado.";
            }
            else if (string.Equals(result.Status, "DowngradeRejected", StringComparison.OrdinalIgnoreCase))
            {
                IsUpdateAvailable = false;
                UpdateStatusMessage = "Versão remota inferior à versão atual (downgrade recusado).";
            }
            else
            {
                IsUpdateAvailable = false;
                UpdateStatusMessage = !string.IsNullOrEmpty(result.ErrorMessage)
                    ? $"Não foi possível verificar atualizações: {result.ErrorMessage}"
                    : "Não foi possível verificar atualizações. O aplicativo funciona normalmente offline.";
            }

            LastCheckDisplay = DateTime.Now.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.CurrentCulture);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = $"Erro ao verificar: {ex.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    public async Task ApplyUpdateAsync()
    {
        UpdateStatusMessage = "Iniciando instalador da atualização...";
        await _updateCoordinator.LaunchUpdateInstallerAsync();
    }

    public bool RegisterFileAssociations()
    {
        var exePath = Environment.ProcessPath ?? string.Empty;
        if (!string.IsNullOrEmpty(exePath) && FileAssociationManager.RegisterAssociations(exePath))
        {
            StatusMessage = _localizationService?["Settings_Explorer_Assoc_Registered_Success"]
                ?? "Registered Zip Buddy for all supported archive file types.";
            return true;
        }

        StatusMessage = _localizationService?["Settings_Explorer_Assoc_Registered_Failed"]
            ?? "Could not register file associations in Windows Registry.";
        return false;
    }

    public bool UnregisterFileAssociations()
    {
        if (FileAssociationManager.UnregisterAssociations())
        {
            StatusMessage = _localizationService?["Settings_Explorer_Assoc_Unregistered_Success"]
                ?? "Unregistered Zip Buddy file associations.";
            return true;
        }

        StatusMessage = _localizationService?["Settings_Explorer_Assoc_Unregistered_Failed"]
            ?? "Could not remove file associations from Windows Registry.";
        return false;
    }

    public void OpenDefaultAppsSettings()
    {
        RegisterFileAssociations();
        FileAssociationManager.OpenDefaultAppsSettings();
        StatusMessage = _localizationService?["Settings_Explorer_DefaultApps_Launched"]
            ?? "Windows Settings opened. Select Zip Buddy as your default archive application.";
    }

    public void OpenLogFolder()
    {
        var logDir = _loggingService.GetLogDirectory();
        ShellOperations.OpenFolderAndSelect(logDir);
    }
}
