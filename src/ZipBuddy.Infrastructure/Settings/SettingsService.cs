using System.Text.Json;
using ZipBuddy.Core.Interfaces;
using ZipBuddy.Core.Models;

namespace ZipBuddy.Infrastructure.Settings;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsFilePath;
    private AppSettings _settings;

    public string DefaultExtractionFolder
    {
        get => _settings.DefaultExtractionFolder;
        set => _settings.DefaultExtractionFolder = value;
    }

    public bool OpenExtractedFolderAfterExtraction
    {
        get => _settings.OpenExtractedFolderAfterExtraction;
        set => _settings.OpenExtractedFolderAfterExtraction = value;
    }

    public bool KeepTaskProgressWindowOpen
    {
        get => _settings.KeepTaskProgressWindowOpen;
        set
        {
            _settings.KeepTaskProgressWindowOpen = value;
            _settings.AutoCloseTaskProgressWindow = !value;
        }
    }

    public bool DeleteArchiveAfterExtraction
    {
        get => _settings.DeleteArchiveAfterExtraction;
        set => _settings.DeleteArchiveAfterExtraction = value;
    }

    public string DoubleClickAction
    {
        get => _settings.DoubleClickAction;
        set => _settings.DoubleClickAction = value;
    }

    public bool ConfirmBeforeOverwriting
    {
        get => _settings.ConfirmBeforeOverwriting;
        set => _settings.ConfirmBeforeOverwriting = value;
    }

    public bool ConfirmBeforeDeleting
    {
        get => _settings.ConfirmBeforeDeleting;
        set => _settings.ConfirmBeforeDeleting = value;
    }

    public ArchiveFormat DefaultArchiveFormat
    {
        get => Enum.IsDefined(typeof(ArchiveFormat), _settings.DefaultArchiveFormat) && _settings.DefaultArchiveFormat != ArchiveFormat.Unknown
            ? _settings.DefaultArchiveFormat
            : ArchiveFormat.Zip;
        set
        {
            var format = Enum.IsDefined(typeof(ArchiveFormat), value) && value != ArchiveFormat.Unknown
                ? value
                : ArchiveFormat.Zip;
            _settings.DefaultArchiveFormat = format;
            _settings.DefaultCompressionLevel = format.GetCompatibleCompressionLevel(_settings.DefaultCompressionLevel);
        }
    }

    public CompressionLevel DefaultCompressionLevel
    {
        get
        {
            var level = Enum.IsDefined(typeof(CompressionLevel), _settings.DefaultCompressionLevel)
                ? _settings.DefaultCompressionLevel
                : CompressionLevel.Normal;
            return DefaultArchiveFormat.GetCompatibleCompressionLevel(level);
        }
        set
        {
            var level = Enum.IsDefined(typeof(CompressionLevel), value)
                ? value
                : CompressionLevel.Normal;
            _settings.DefaultCompressionLevel = DefaultArchiveFormat.GetCompatibleCompressionLevel(level);
        }
    }

    public string ThemeMode
    {
        get => _settings.ThemeMode;
        set => _settings.ThemeMode = value;
    }

    public string Language
    {
        get => _settings.Language;
        set => _settings.Language = value;
    }

    public string CustomTempDirectory
    {
        get => _settings.CustomTempDirectory;
        set => _settings.CustomTempDirectory = value;
    }

    public int MaxExtractionThreads
    {
        get => _settings.MaxExtractionThreads >= 1 && _settings.MaxExtractionThreads <= 64 ? _settings.MaxExtractionThreads : Math.Clamp(Environment.ProcessorCount, 1, 64);
        set => _settings.MaxExtractionThreads = value >= 1 && value <= 64 ? value : Math.Clamp(Environment.ProcessorCount, 1, 64);
    }

    public bool EnableContextMenu
    {
        get => _settings.EnableContextMenu;
        set => _settings.EnableContextMenu = value;
    }

    public bool UseCascadingContextMenu
    {
        get => _settings.UseCascadingContextMenu;
        set => _settings.UseCascadingContextMenu = value;
    }

    [Obsolete("Use KeepTaskProgressWindowOpen instead.")]
    public bool AutoCloseTaskProgressWindow
    {
        get => !_settings.KeepTaskProgressWindowOpen;
        set => KeepTaskProgressWindowOpen = !value;
    }

    public bool NotifyOnTaskCompletion
    {
        get => _settings.NotifyOnTaskCompletion;
        set => _settings.NotifyOnTaskCompletion = value;
    }

    public IReadOnlyList<string> EnabledContextMenuCommands
    {
        get => _settings.EnabledContextMenuCommands;
        set => _settings.EnabledContextMenuCommands = value.ToList();
    }

    public IReadOnlyList<string> AssociatedExtensions
    {
        get => _settings.AssociatedExtensions;
        set => _settings.AssociatedExtensions = value.ToList();
    }

    public string LogLevel
    {
        get => _settings.LogLevel;
        set => _settings.LogLevel = value;
    }

    public bool AutoCheckUpdates
    {
        get => _settings.AutoCheckUpdates;
        set => _settings.AutoCheckUpdates = value;
    }

    public string UpdateCheckFrequency
    {
        get => _settings.UpdateCheckFrequency;
        set => _settings.UpdateCheckFrequency = value;
    }

    public DateTime? LastUpdateCheckTime
    {
        get => _settings.LastUpdateCheckTime;
        set => _settings.LastUpdateCheckTime = value;
    }

    public string LastUpdateCheckResult
    {
        get => _settings.LastUpdateCheckResult;
        set => _settings.LastUpdateCheckResult = value;
    }

    private readonly ILoggingService? _logger;
    private bool _preventSaveDueToUnsafeBackup;

    public string? LastCorruptBackupPath { get; private set; }

    public SettingsService(ILoggingService? logger = null)
        : this(null, logger)
    {
    }

    public SettingsService(string? customSettingsFilePath, ILoggingService? logger = null)
    {
        _logger = logger;
        if (!string.IsNullOrEmpty(customSettingsFilePath))
        {
            _settingsFilePath = customSettingsFilePath;
        }
        else
        {
            // Isolated user preferences directory: %APPDATA%\Zip Buddy (Roaming)
            // Preserved across complete uninstallation, clean reinstallation, and updates.
            var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(roamingAppData, "Zip Buddy");
            Directory.CreateDirectory(folder);
            _settingsFilePath = Path.Combine(folder, "settings.json");

            // Migration check: If LocalApplicationData exists but Roaming doesn't, migrate it seamlessly
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var legacyFilePath = Path.Combine(localAppData, "Zip Buddy", "settings.json");
            if (!File.Exists(_settingsFilePath) && File.Exists(legacyFilePath))
            {
                try
                {
                    File.Copy(legacyFilePath, _settingsFilePath, true);
                }
                catch (Exception ex)
                {
                    _logger?.Warn($"Could not migrate legacy settings from LocalAppData: {ex.Message}");
                }
            }
        }

        _settings = new AppSettings();
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    // Migration precedence: based on JSON property presence
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        bool hasAuth = false;
                        bool authVal = false;
                        bool hasLegacy = false;
                        bool legacyVal = false;

                        foreach (var prop in root.EnumerateObject())
                        {
                            if (prop.Name.Equals(nameof(AppSettings.KeepTaskProgressWindowOpen), StringComparison.OrdinalIgnoreCase))
                            {
                                hasAuth = true;
                                if (prop.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                                    authVal = prop.Value.GetBoolean();
                            }
                            else if (prop.Name.Equals(nameof(AppSettings.AutoCloseTaskProgressWindow), StringComparison.OrdinalIgnoreCase))
                            {
                                hasLegacy = true;
                                if (prop.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                                    legacyVal = prop.Value.GetBoolean();
                            }
                        }

                        if (hasAuth)
                        {
                            // Authoritative property wins unconditionally
                            loaded.KeepTaskProgressWindowOpen = authVal;
                        }
                        else if (hasLegacy)
                        {
                            // Only legacy property exists: derive authoritative value
                            loaded.KeepTaskProgressWindowOpen = !legacyVal;
                        }
                        else
                        {
                            // Documented default
                            loaded.KeepTaskProgressWindowOpen = false;
                        }

                        // Synchronize legacy property for backward compatibility on disk
                        loaded.AutoCloseTaskProgressWindow = !loaded.KeepTaskProgressWindowOpen;
                    }

                    loaded.DefaultCompressionLevel = loaded.DefaultArchiveFormat.GetCompatibleCompressionLevel(loaded.DefaultCompressionLevel);
                    _settings = loaded;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger?.Warn($"Settings file contains malformed JSON: {ex.Message}");
            HandleCorruptedSettingsFile();
            _settings = new AppSettings();
        }
        catch (Exception ex)
        {
            _logger?.Error($"Settings file could not be read due to an I/O or access error: {ex.Message}");
            _settings = new AppSettings();
        }
    }

    public bool SimulateBackupFailure { get; set; }

    private void HandleCorruptedSettingsFile()
    {
        try
        {
            if (SimulateBackupFailure)
            {
                throw new IOException("Simulated backup failure for verification.");
            }

            if (File.Exists(_settingsFilePath))
            {
                var backupPath = $"{_settingsFilePath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}.bak";
                File.Copy(_settingsFilePath, backupPath, true);

                if (File.Exists(backupPath) && new FileInfo(backupPath).Length == new FileInfo(_settingsFilePath).Length)
                {
                    LastCorruptBackupPath = backupPath;
                    _preventSaveDueToUnsafeBackup = false;
                    _logger?.Warn($"Corrupted settings file safely preserved at '{Path.GetFileName(backupPath)}'. Defaults loaded.");
                }
                else
                {
                    _preventSaveDueToUnsafeBackup = true;
                    _logger?.Error("Failed to verify backup of corrupted settings file. Save operations will be prevented to avoid data loss.");
                }
            }
        }
        catch (Exception ex)
        {
            _preventSaveDueToUnsafeBackup = true;
            _logger?.Error($"Exception creating backup of corrupted settings file: {ex.Message}. Save operations will be prevented to avoid data loss.");
        }
    }

    public async Task<bool> SaveAsync()
    {
        if (_preventSaveDueToUnsafeBackup && File.Exists(_settingsFilePath))
        {
            _logger?.Error("Settings save aborted: corrupted original settings file could not be backed up safely.");
            return false;
        }

        try
        {
            var folder = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _settings.DefaultCompressionLevel = DefaultArchiveFormat.GetCompatibleCompressionLevel(_settings.DefaultCompressionLevel);

            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            var tempFile = _settingsFilePath + ".tmp";
            await File.WriteAllTextAsync(tempFile, json);
            File.Move(tempFile, _settingsFilePath, true);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to persist settings: {ex.Message}");
            return false;
        }
    }
}
